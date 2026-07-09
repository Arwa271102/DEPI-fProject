using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Apartment;
using Sakanak.BLL.DTOs.Common;
using Sakanak.BLL.Interfaces;
using Sakanak.BLL.Options;
using Sakanak.DAL.Data;
using Sakanak.DAL.UnitOfWork;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Services;

/// <summary>
/// Handles landlord apartment lifecycle rules for Phase 1 inventory management.
/// </summary>
public class ApartmentService : IApartmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMediaService _mediaService;
    private readonly IEmailService _emailService;
    private readonly SakanakDbContext _dbContext;
    private readonly IValidator<CreateApartmentDto> _createValidator;
    private readonly IValidator<UpdateApartmentDto> _updateValidator;
    private readonly BusinessRuleOptions _businessRules;
    private readonly INotificationService _notificationService;

    public ApartmentService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMediaService mediaService,
        IEmailService emailService,
        SakanakDbContext dbContext,
        IValidator<CreateApartmentDto> createValidator,
        IValidator<UpdateApartmentDto> updateValidator,
        Microsoft.Extensions.Options.IOptions<BusinessRuleOptions> businessRules,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _mediaService = mediaService;
        _emailService = emailService;
        _dbContext = dbContext;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _businessRules = businessRules.Value;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Creates an apartment, uploads its media, and generates the initial pending approval request.
    /// </summary>
    public async Task<Result<int>> CreateApartmentAsync(CreateApartmentDto dto, Guid applicationUserId)
    {
        var landlord = await _unitOfWork.Landlords.GetByUserIdAsync(applicationUserId);
        if (landlord is null)
        {
            return Result<int>.Failure("Landlord profile was not found.");
        }

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Result<int>.Failure(validation.Errors.Select(error => error.ErrorMessage));
        }

        var fileValidation = _mediaService.ValidateApartmentFiles(dto.Photos);
        if (!fileValidation.Succeeded)
        {
            return Result<int>.Failure(fileValidation.Errors);
        }

        var apartment = _mapper.Map<Apartment>(dto);
        apartment.LandlordId = landlord.LandlordId;
        apartment.IsActive = false;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.Apartments.AddAsync(apartment);
            await _unitOfWork.SaveChangesAsync();

            var apartmentGroup = new ApartmentGroup
            {
                ApartmentId = apartment.ApartmentId,
                MaxMembers = apartment.TotalSeats,
                GroupName = $"Group for {apartment.Address}",
                GroupStatus = GroupStatus.Open,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.ApartmentGroups.AddAsync(apartmentGroup);
            await _unitOfWork.SaveChangesAsync();

            var mediaResult = await _mediaService.UploadApartmentPhotosAsync(apartment.ApartmentId, dto.Photos);
            if (!mediaResult.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<int>.Failure(mediaResult.Errors);
            }

            var request = new Request
            {
                ApartmentId = apartment.ApartmentId,
                LandlordId = landlord.LandlordId,
                CreatedAt = DateTime.UtcNow,
                Message = "Apartment submitted for admin review.",
                Type = RequestType.ApartmentUpload,
                Status = RequestStatus.Pending
            };

            await _unitOfWork.Requests.AddAsync(request);
            landlord.TotalProperties = await _unitOfWork.Landlords.GetTotalPropertiesCountAsync(landlord.LandlordId);
            await _unitOfWork.Landlords.UpdateAsync(landlord);

            await _unitOfWork.CommitTransactionAsync();

            await NotifyAdminsAboutNewApartmentAsync(landlord, apartment);
            return Result<int>.Success(apartment.ApartmentId);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    /// <summary>
    /// Returns the apartments owned by the current landlord with their latest workflow status.
    /// </summary>
    public async Task<Result<PagedResult<ApartmentListItemDto>>> GetLandlordApartmentsAsync(
        Guid applicationUserId,
        int pageNumber = 1,
        int pageSize = 10,
        string? city = null,
        string? requestStatus = null,
        string sortBy = "created",
        bool descending = true)
    {
        var landlord = await _unitOfWork.Landlords.GetByUserIdAsync(applicationUserId);
        if (landlord is null)
        {
            return Result<PagedResult<ApartmentListItemDto>>.Failure("Landlord profile was not found.");
        }

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _unitOfWork.Apartments.GetApartmentsWithDetailsQuery().AsNoTracking()
            .Where(apartment => apartment.LandlordId == landlord.LandlordId);

        if (!string.IsNullOrWhiteSpace(city))
        {
            var normalizedCity = city.Trim().ToLower();
            query = query.Where(apartment => apartment.City.ToLower().Contains(normalizedCity));
        }

        if (!string.IsNullOrWhiteSpace(requestStatus))
        {
            if (Enum.TryParse<RequestStatus>(requestStatus.Trim(), true, out var parsedStatus))
            {
                query = query.Where(apartment =>
                    apartment.Requests.OrderByDescending(request => request.CreatedAt)
                        .Select(request => (RequestStatus?)request.Status)
                        .FirstOrDefault() == parsedStatus);
            }
        }

        query = (sortBy.ToLowerInvariant(), descending) switch
        {
            ("address", false) => query.OrderBy(apartment => apartment.Address),
            ("address", true) => query.OrderByDescending(apartment => apartment.Address),
            ("city", false) => query.OrderBy(apartment => apartment.City),
            ("city", true) => query.OrderByDescending(apartment => apartment.City),
            ("price", false) => query.OrderBy(apartment => apartment.PricePerMonth),
            ("price", true) => query.OrderByDescending(apartment => apartment.PricePerMonth),
            ("status", false) => query.OrderBy(apartment => apartment.IsActive),
            ("status", true) => query.OrderByDescending(apartment => apartment.IsActive),
            ("requeststatus", false) => query.OrderBy(apartment => apartment.Requests.OrderByDescending(request => request.CreatedAt).Select(request => request.Status).FirstOrDefault()),
            ("requeststatus", true) => query.OrderByDescending(apartment => apartment.Requests.OrderByDescending(request => request.CreatedAt).Select(request => request.Status).FirstOrDefault()),
            (_, false) => query.OrderBy(apartment => apartment.ApartmentId),
            _ => query.OrderByDescending(apartment => apartment.ApartmentId)
        };

        var totalCount = await query.CountAsync();
        var apartments = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var mappedItems = _mapper.Map<List<ApartmentListItemDto>>(apartments);
        return Result<PagedResult<ApartmentListItemDto>>.Success(new PagedResult<ApartmentListItemDto>
        {
            Items = mappedItems,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Retrieves apartment details, optionally enforcing landlord ownership.
    /// </summary>
    public async Task<Result<ApartmentDetailsDto>> GetApartmentByIdAsync(int apartmentId, Guid applicationUserId, bool requireOwnership = true)
    {
        var landlord = requireOwnership ? await _unitOfWork.Landlords.GetByUserIdAsync(applicationUserId) : null;

        var apartment = await _unitOfWork.Apartments.GetApartmentsWithDetailsQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ApartmentId == apartmentId);

        if (apartment is null)
        {
            return Result<ApartmentDetailsDto>.Failure("Apartment not found.");
        }

        if (requireOwnership && apartment.LandlordId != landlord?.LandlordId)
        {
            return Result<ApartmentDetailsDto>.Failure("You do not have access to this apartment.");
        }

        return Result<ApartmentDetailsDto>.Success(_mapper.Map<ApartmentDetailsDto>(apartment));
    }

    /// <summary>
    /// Updates an apartment, manages photo changes, and blocks edits while a review is pending.
    /// </summary>
    public async Task<Result> UpdateApartmentAsync(UpdateApartmentDto dto, Guid applicationUserId)
    {
        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Result.Failure(validation.Errors.Select(error => error.ErrorMessage));
        }

        var landlord = await _unitOfWork.Landlords.GetByUserIdAsync(applicationUserId);
        if (landlord is null)
        {
            return Result.Failure("Landlord profile was not found.");
        }

        var apartment = await _unitOfWork.Apartments.GetApartmentsWithDetailsQuery()
            .FirstOrDefaultAsync(item => item.ApartmentId == dto.ApartmentId);

        if (apartment is null || apartment.LandlordId != landlord.LandlordId)
        {
            return Result.Failure("Apartment not found.");
        }

        var latestRequest = apartment.Requests.OrderByDescending(request => request.CreatedAt).FirstOrDefault();
        if (latestRequest?.Status == RequestStatus.Pending)
        {
            return Result.Failure("You cannot edit an apartment while its approval request is pending.");
        }

        var wasApproved = latestRequest?.Status == RequestStatus.Approved;
        var previousValues = wasApproved ? JsonSerializer.Serialize(new
        {
            apartment.Address,
            apartment.City,
            apartment.PricePerMonth,
            apartment.TotalSeats,
            apartment.Amenities,
            apartment.VirtualTourUrl
        }) : null;

        apartment.Address = dto.Address.Trim();
        apartment.City = dto.City.Trim();
        apartment.PricePerMonth = dto.PricePerMonth;
        apartment.TotalSeats = Math.Min(dto.TotalSeats, _businessRules.MaxApartmentSeats);
        apartment.VirtualTourUrl = string.IsNullOrWhiteSpace(dto.VirtualTourUrl) ? null : dto.VirtualTourUrl.Trim();
        apartment.Amenities = dto.Amenities.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        if (dto.RemovedPhotoIds.Count > 0)
        {
            foreach (var mediaId in dto.RemovedPhotoIds.Distinct())
            {
                var media = apartment.Media.FirstOrDefault(item => item.MediaId == mediaId);
                if (media is not null)
                {
                    var deleteResult = await _mediaService.DeleteMediaAsync(mediaId);
                    if (!deleteResult.Succeeded)
                    {
                        return deleteResult;
                    }
                }
            }
        }

        if (dto.NewPhotos?.Count > 0)
        {
            var uploadResult = await _mediaService.UploadApartmentPhotosAsync(apartment.ApartmentId, dto.NewPhotos);
            if (!uploadResult.Succeeded)
            {
                return Result.Failure(uploadResult.Errors);
            }
        }

        var dbPhotoCount = (await _unitOfWork.Media.GetImagesByEntityAsync(MediaService.ApartmentEntityType, apartment.ApartmentId)).Count();
        var navPhotoCount = apartment.Media.Count(m => m.Type == MediaType.Image);
        var totalPhotoCount = Math.Max(dbPhotoCount, navPhotoCount);

        if (totalPhotoCount < _businessRules.MinimumPhotosRequired)
        {
            return Result.Failure($"At least {_businessRules.MinimumPhotosRequired} photo is required.");
        }

        await _unitOfWork.Apartments.UpdateAsync(apartment);
        await _unitOfWork.SaveChangesAsync();

        if (wasApproved)
        {
            var editRequest = new Request
            {
                ApartmentId = apartment.ApartmentId,
                LandlordId = landlord.LandlordId,
                CreatedAt = DateTime.UtcNow,
                Message = "Apartment edits submitted for admin review.",
                Status = RequestStatus.Pending,
                Type = RequestType.ApartmentEdit,
                PreviousValues = previousValues
            };

            apartment.IsActive = false;
            await _unitOfWork.Requests.AddAsync(editRequest);
            await _unitOfWork.Apartments.UpdateAsync(apartment);
            await _unitOfWork.SaveChangesAsync();
            await NotifyAdminsAboutNewApartmentAsync(landlord, apartment);
        }
        else if (latestRequest?.Status is RequestStatus.Rejected or RequestStatus.Cancelled)
        {
            var newRequest = new Request
            {
                ApartmentId = apartment.ApartmentId,
                LandlordId = landlord.LandlordId,
                CreatedAt = DateTime.UtcNow,
                Message = "Apartment resubmitted after updates.",
                Type = RequestType.ApartmentUpload,
                Status = RequestStatus.Pending
            };

            apartment.IsActive = false;
            await _unitOfWork.Requests.AddAsync(newRequest);
            await _unitOfWork.SaveChangesAsync();
            await NotifyAdminsAboutNewApartmentAsync(landlord, apartment);
        }

        return Result.Success();
    }

    /// <summary>
    /// Toggles landlord-controlled availability for approved apartments.
    /// </summary>
    public async Task<Result> ToggleApartmentActiveStatusAsync(int apartmentId, Guid applicationUserId)
    {
        var landlord = await _unitOfWork.Landlords.GetByUserIdAsync(applicationUserId);
        if (landlord is null)
        {
            return Result.Failure("Landlord profile was not found.");
        }

        var apartment = await _unitOfWork.Apartments.GetApartmentsWithDetailsQuery()
            .FirstOrDefaultAsync(item => item.ApartmentId == apartmentId);

        if (apartment is null || apartment.LandlordId != landlord.LandlordId)
        {
            return Result.Failure("Apartment not found.");
        }

        var latestRequest = apartment.Requests.OrderByDescending(request => request.CreatedAt).FirstOrDefault();
        if (latestRequest?.Status != RequestStatus.Approved)
        {
            return Result.Failure("Only approved apartments can be activated or deactivated.");
        }

        var isDeactivating = apartment.IsActive;
        if (isDeactivating)
        {
            var hasActiveContracts = await _dbContext.Contracts
                .AnyAsync(c => c.ApartmentId == apartmentId && c.Status == ContractStatus.Active);

            if (hasActiveContracts)
            {
                return Result.Failure("Cannot deactivate apartment with active paid contracts. Students are currently living there.");
            }

            var activeBookings = await _dbContext.Bookings
                .Include(booking => booking.Student)
                    .ThenInclude(student => student.ApplicationUser)
                .Where(booking =>
                    booking.ApartmentId == apartmentId &&
                    (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Accepted))
                .ToListAsync();

            foreach (var booking in activeBookings)
            {
                booking.Status = BookingStatus.Rejected;
                booking.RejectedAt = DateTime.UtcNow;
                booking.RejectionReason = "This apartment is no longer available.";
            }

            var activeContracts = await _dbContext.Contracts
                .Include(contract => contract.Student)
                    .ThenInclude(student => student.ApplicationUser)
                .Where(contract =>
                    contract.ApartmentId == apartmentId &&
                    (contract.Status == ContractStatus.PendingApproval || contract.Status == ContractStatus.Approved))
                .ToListAsync();

            foreach (var contract in activeContracts)
            {
                contract.Status = ContractStatus.Rejected;
                contract.ReviewedAt = DateTime.UtcNow;
                contract.RejectionReason = "This apartment is no longer available.";
            }

            apartment.IsActive = false;
            await _unitOfWork.Apartments.UpdateAsync(apartment);
            await _unitOfWork.SaveChangesAsync();

            foreach (var booking in activeBookings)
            {
                await NotifyStudentApartmentUnavailableAsync(booking, apartment);
            }

            foreach (var contract in activeContracts)
            {
                await NotifyStudentContractUnavailableAsync(contract, apartment);
            }

            return Result.Success();
        }

        apartment.IsActive = true;
        await _unitOfWork.Apartments.UpdateAsync(apartment);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    /// <summary>
    /// Deletes an apartment when it has no related bookings or contracts.
    /// </summary>
    public async Task<Result> DeleteApartmentAsync(int apartmentId, Guid applicationUserId)
    {
        var landlord = await _unitOfWork.Landlords.GetByUserIdAsync(applicationUserId);
        if (landlord is null)
        {
            return Result.Failure("Landlord profile was not found.");
        }

        var apartment = await _unitOfWork.Apartments.GetApartmentForDeletionAsync(apartmentId);

        if (apartment is null || apartment.LandlordId != landlord.LandlordId)
        {
            return Result.Failure("Apartment not found.");
        }

        if (apartment.Bookings.Count > 0 || apartment.Contracts.Count > 0)
        {
            return Result.Failure("Apartments with bookings or contracts cannot be deleted.");
        }

        await _mediaService.DeleteApartmentMediaAsync(apartment.ApartmentId);
        await _unitOfWork.Requests.DeleteByApartmentAsync(apartment.ApartmentId);
        await _unitOfWork.Apartments.DeleteAsync(apartment.ApartmentId);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    /// <summary>
    /// Checks whether a specific apartment belongs to the current landlord.
    /// </summary>
    public async Task<Result<bool>> ApartmentBelongsToLandlordAsync(int apartmentId, Guid applicationUserId)
    {
        var landlord = await _unitOfWork.Landlords.GetByUserIdAsync(applicationUserId);
        if (landlord is null)
        {
            return Result<bool>.Failure("Landlord profile was not found.");
        }

        var apartment = await _unitOfWork.Apartments.GetByIdAsync(apartmentId);
        return Result<bool>.Success(apartment?.LandlordId == landlord.LandlordId);
    }

    private async Task NotifyAdminsAboutNewApartmentAsync(Landlord landlord, Apartment apartment)
    {
        var admins = await _unitOfWork.Admins.GetAdminsWithUsersQuery()
            .Where(admin => admin.ApplicationUser.Email != null)
            .ToListAsync();

        if (admins.Count == 0)
        {
            return;
        }

        var landlordEmail = (await _unitOfWork.Landlords.GetLandlordsWithDetailsQuery()
            .FirstAsync(item => item.LandlordId == landlord.LandlordId)).ApplicationUser.Email ?? "unknown";

        foreach (var admin in admins)
        {
            await _emailService.SendEmailAsync(
                admin.ApplicationUser.Email!,
                "New apartment upload request",
                $"New apartment upload request from {landlordEmail} for {apartment.Address}, {apartment.City}.");
            await _notificationService.CreateNotificationAsync(
                admin.ApplicationUserId,
                "New Apartment Request",
                $"{landlordEmail} uploaded {apartment.Address}, {apartment.City}.",
                NotificationType.NewApartmentRequest,
                "/Admin/PendingRequests");
        }
    }

    private async Task NotifyStudentApartmentUnavailableAsync(Booking booking, Apartment apartment)
    {
        var email = booking.Student.ApplicationUser.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        await _emailService.SendEmailAsync(
            email,
            "Booking cancelled - apartment no longer available",
            $"<p>Your booking for {apartment.Address}, {apartment.City} was cancelled because the apartment is no longer available.</p><p>Please search for another apartment on Sakanak.</p>");
    }

    private async Task NotifyStudentContractUnavailableAsync(Contract contract, Apartment apartment)
    {
        var email = contract.Student.ApplicationUser.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        await _emailService.SendEmailAsync(
            email,
            "Contract rejected - apartment no longer available",
            $"<p>Your contract for {apartment.Address}, {apartment.City} was rejected because the apartment is no longer available.</p><p>Please search for another apartment on Sakanak.</p>");
    }
}
