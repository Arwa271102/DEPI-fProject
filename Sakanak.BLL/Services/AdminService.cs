using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Admin;
using Sakanak.BLL.DTOs.Apartment;
using Sakanak.BLL.DTOs.Common;
using Sakanak.BLL.Interfaces;
using Sakanak.DAL.Data;
using Sakanak.DAL.UnitOfWork;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Services;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;

    public AdminService(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IMapper mapper,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _emailService = emailService;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    public async Task<Result<PagedResult<LandlordListItemDto>>> GetAllLandlordsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? search = null,
        string? status = null,
        string sortBy = "registered",
        bool descending = true)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _unitOfWork.Landlords.GetLandlordsWithDetailsQuery().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(landlord =>
                landlord.ApplicationUser.Name.ToLower().Contains(normalizedSearch) ||
                (landlord.ApplicationUser.Email ?? string.Empty).ToLower().Contains(normalizedSearch));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<UserStatus>(status.Trim(), true, out var parsedStatus))
        {
            query = query.Where(landlord => landlord.ApplicationUser.Status == parsedStatus);
        }

        query = (sortBy.ToLowerInvariant(), descending) switch
        {
            ("name", false) => query.OrderBy(landlord => landlord.ApplicationUser.Name),
            ("name", true) => query.OrderByDescending(landlord => landlord.ApplicationUser.Name),
            ("email", false) => query.OrderBy(landlord => landlord.ApplicationUser.Email),
            ("email", true) => query.OrderByDescending(landlord => landlord.ApplicationUser.Email),
            ("status", false) => query.OrderBy(landlord => landlord.ApplicationUser.Status),
            ("status", true) => query.OrderByDescending(landlord => landlord.ApplicationUser.Status),
            ("apartments", false) => query.OrderBy(landlord => landlord.Apartments.Count),
            ("apartments", true) => query.OrderByDescending(landlord => landlord.Apartments.Count),
            ("registered", false) => query.OrderBy(landlord => landlord.ApplicationUser.RegistrationDate),
            _ => query.OrderByDescending(landlord => landlord.ApplicationUser.RegistrationDate)
        };

        var totalCount = await query.CountAsync();
        var landlords = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = landlords.Select(landlord => new LandlordListItemDto
        {
            LandlordId = landlord.LandlordId,
            Name = landlord.ApplicationUser.Name,
            Email = landlord.ApplicationUser.Email ?? string.Empty,
            PhoneNumber = landlord.ApplicationUser.PhoneNumber,
            VerificationStatus = landlord.VerificationStatus,
            TotalApartments = landlord.Apartments.Count,
            ActiveApartments = landlord.Apartments.Count(apartment => apartment.IsActive),
            Status = landlord.ApplicationUser.Status.ToString(),
            RegistrationDate = landlord.ApplicationUser.RegistrationDate
        }).ToList();

        return Result<PagedResult<LandlordListItemDto>>.Success(new PagedResult<LandlordListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    public async Task<Result<LandlordDetailsDto>> GetLandlordDetailsAsync(int landlordId)
    {
        var landlord = await _unitOfWork.Landlords.GetLandlordsWithDetailsQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.LandlordId == landlordId);

        if (landlord is null)
        {
            return Result<LandlordDetailsDto>.Failure("Landlord not found.");
        }

        var dto = new LandlordDetailsDto
        {
            LandlordId = landlord.LandlordId,
            Name = landlord.ApplicationUser.Name,
            Email = landlord.ApplicationUser.Email ?? string.Empty,
            PhoneNumber = landlord.ApplicationUser.PhoneNumber,
            Age = landlord.Age,
            VerificationStatus = landlord.VerificationStatus,
            Status = landlord.ApplicationUser.Status.ToString(),
            RegistrationDate = landlord.ApplicationUser.RegistrationDate,
            TotalApartments = landlord.Apartments.Count,
            ActiveApartments = landlord.Apartments.Count(apartment => apartment.IsActive),
            Apartments = landlord.Apartments
                .OrderByDescending(apartment => apartment.ApartmentId)
                .Select(apartment => new LandlordManagedApartmentDto
                {
                    ApartmentId = apartment.ApartmentId,
                    Address = apartment.Address,
                    City = apartment.City,
                    PricePerMonth = apartment.PricePerMonth,
                    IsActive = apartment.IsActive,
                    RequestStatus = apartment.Requests.OrderByDescending(request => request.CreatedAt).Select(request => request.Status.ToString()).FirstOrDefault() ?? "No Request"
                })
                .ToList()
        };

        return Result<LandlordDetailsDto>.Success(dto);
    }

    public async Task<Result<PagedResult<LandlordVerificationRequestDto>>> GetPendingVerificationsAsync(int pageNumber = 1, int pageSize = 10)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _unitOfWork.Landlords.GetLandlordsWithDetailsQuery()
            .AsNoTracking()
            .Where(landlord => !landlord.VerificationStatus && landlord.VerificationRequestedAt != null && landlord.RejectionReason == null)
            .OrderBy(landlord => landlord.VerificationRequestedAt);

        var totalCount = await query.CountAsync();
        var landlords = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return Result<PagedResult<LandlordVerificationRequestDto>>.Success(new PagedResult<LandlordVerificationRequestDto>
        {
            Items = landlords.Select(MapLandlordVerification).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    public async Task<Result<PagedResult<LandlordVerificationRequestDto>>> GetAllVerificationsAsync(int pageNumber = 1, int pageSize = 10, string? status = null)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 50);

        // Only include landlords who have submitted verification (VerificationRequestedAt != null)
        var query = _unitOfWork.Landlords.GetLandlordsWithDetailsQuery()
            .AsNoTracking()
            .Where(landlord => landlord.VerificationRequestedAt != null)
            .OrderByDescending(landlord => landlord.VerificationRequestedAt);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                query = (IOrderedQueryable<Sakanak.Domain.Entities.Landlord>)query.Where(l => l.VerificationStatus);
            else if (status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                query = (IOrderedQueryable<Sakanak.Domain.Entities.Landlord>)query.Where(l => !l.VerificationStatus && l.RejectionReason != null);
            else if (status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                query = (IOrderedQueryable<Sakanak.Domain.Entities.Landlord>)query.Where(l => !l.VerificationStatus && l.RejectionReason == null);
        }

        var totalCount = await query.CountAsync();
        var landlords = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return Result<PagedResult<LandlordVerificationRequestDto>>.Success(new PagedResult<LandlordVerificationRequestDto>
        {
            Items = landlords.Select(MapLandlordVerification).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }


    public async Task<Result<LandlordVerificationRequestDto>> GetLandlordVerificationAsync(int landlordId)
    {
        var landlord = await _unitOfWork.Landlords.GetLandlordsWithDetailsQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.LandlordId == landlordId);

        if (landlord is null)
        {
            return Result<LandlordVerificationRequestDto>.Failure("Landlord not found.");
        }

        return Result<LandlordVerificationRequestDto>.Success(MapLandlordVerification(landlord));
    }

    public async Task<Result> ApproveLandlordAsync(int landlordId, Guid adminApplicationUserId)
    {
        var admin = await _unitOfWork.Admins.GetByUserIdAsync(adminApplicationUserId);
        if (admin is null)
        {
            return Result.Failure("Admin profile was not found.");
        }

        var landlord = await _unitOfWork.Landlords.GetLandlordsWithDetailsQuery()
            .FirstOrDefaultAsync(item => item.LandlordId == landlordId);

        if (landlord is null)
        {
            return Result.Failure("Landlord not found.");
        }

        landlord.VerificationStatus = true;
        landlord.VerifiedAt = DateTime.UtcNow;
        landlord.VerifiedByAdminId = admin.AdminId;
        landlord.RejectionReason = null;
        await _unitOfWork.Landlords.UpdateAsync(landlord);
        await _unitOfWork.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(landlord.ApplicationUser.Email))
        {
            await _emailService.SendEmailAsync(
                landlord.ApplicationUser.Email!,
                "Your landlord account has been verified",
                "Your Sakanak landlord account has been verified. You can now create apartment listings.");
        }

        await _notificationService.CreateNotificationAsync(
            landlord.ApplicationUserId,
            "Landlord Verified",
            "Your landlord account has been verified. You can now create apartment listings.",
            NotificationType.LandlordVerified,
            "/Landlord/Dashboard");

        return Result.Success();
    }

    public async Task<Result> RejectLandlordAsync(int landlordId, Guid adminApplicationUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure("Rejection reason is required.");
        }

        var admin = await _unitOfWork.Admins.GetByUserIdAsync(adminApplicationUserId);
        if (admin is null)
        {
            return Result.Failure("Admin profile was not found.");
        }

        var landlord = await _unitOfWork.Landlords.GetLandlordsWithDetailsQuery()
            .FirstOrDefaultAsync(item => item.LandlordId == landlordId);

        if (landlord is null)
        {
            return Result.Failure("Landlord not found.");
        }

        landlord.VerificationStatus = false;
        landlord.VerifiedAt = null;
        landlord.VerifiedByAdminId = admin.AdminId;
        landlord.RejectionReason = reason.Trim();
        await _unitOfWork.Landlords.UpdateAsync(landlord);
        await _unitOfWork.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(landlord.ApplicationUser.Email))
        {
            await _emailService.SendEmailAsync(
                landlord.ApplicationUser.Email!,
                "Your landlord verification was rejected",
                $"Your Sakanak landlord verification was rejected. Reason: {landlord.RejectionReason}");
        }

        await _notificationService.CreateNotificationAsync(
            landlord.ApplicationUserId,
            "Landlord Verification Rejected",
            $"Your landlord verification was rejected. Reason: {landlord.RejectionReason}",
            NotificationType.LandlordRejected,
            "/Landlord/VerificationPending");

        return Result.Success();
    }

    public async Task<Result> SuspendLandlordAsync(int landlordId, Guid adminApplicationUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure("Suspension reason is required.");
        }

        var admin = await _unitOfWork.Admins.GetByUserIdAsync(adminApplicationUserId);
        if (admin is null)
        {
            return Result.Failure("Admin profile was not found.");
        }

        var landlord = await _unitOfWork.Landlords.GetLandlordsWithDetailsQuery()
            .FirstOrDefaultAsync(item => item.LandlordId == landlordId);

        if (landlord is null)
        {
            return Result.Failure("Landlord not found.");
        }

        landlord.ApplicationUser.Status = UserStatus.Suspended;

        foreach (var apartment in landlord.Apartments)
        {
            apartment.IsActive = false;
            var pendingRequests = apartment.Requests.Where(request => request.Status == RequestStatus.Pending).ToList();
            foreach (var request in pendingRequests)
            {
                request.Status = RequestStatus.Cancelled;
                request.Message = $"Cancelled by admin due to landlord suspension. Reason: {reason.Trim()}";
                request.ReviewedByAdminId = admin.AdminId;
                request.ResolvedAt = DateTime.UtcNow;
            }
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var userUpdate = await _userManager.UpdateAsync(landlord.ApplicationUser);
            if (!userUpdate.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result.Failure(userUpdate.Errors.Select(error => error.Description));
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        if (!string.IsNullOrWhiteSpace(landlord.ApplicationUser.Email))
        {
            await _emailService.SendEmailAsync(
                landlord.ApplicationUser.Email!,
                "Your landlord account has been suspended",
                $"Your Sakanak landlord account has been suspended. Reason: {reason.Trim()}");
        }

        return Result.Success();
    }

    private static LandlordVerificationRequestDto MapLandlordVerification(Sakanak.Domain.Entities.Landlord landlord)
        => new()
        {
            LandlordId = landlord.LandlordId,
            LandlordName = landlord.ApplicationUser.Name,
            Email = landlord.ApplicationUser.Email ?? string.Empty,
            PhoneNumber = landlord.ApplicationUser.PhoneNumber,
            VerificationRequestedAt = landlord.VerificationRequestedAt,
            Status = landlord.VerificationStatus ? "Verified" : string.IsNullOrWhiteSpace(landlord.RejectionReason) ? "Pending" : "Rejected",
            RejectionReason = landlord.RejectionReason,
            Documents = landlord.Media
                .Where(media => media.EntityType == LandlordVerificationService.EntityType)
                .Select(media => new ApartmentMediaDto { MediaId = media.MediaId, Url = media.Url, Type = media.Type.ToString() })
                .ToList()
        };

    public async Task<Result> ReactivateLandlordAsync(int landlordId, Guid adminApplicationUserId)
    {
        var admin = await _unitOfWork.Admins.GetByUserIdAsync(adminApplicationUserId);
        if (admin is null)
        {
            return Result.Failure("Admin profile was not found.");
        }

        var landlord = await _unitOfWork.Landlords.GetLandlordsWithDetailsQuery()
            .FirstOrDefaultAsync(item => item.LandlordId == landlordId);

        if (landlord is null)
        {
            return Result.Failure("Landlord not found.");
        }

        landlord.ApplicationUser.Status = UserStatus.Active;
        var updateResult = await _userManager.UpdateAsync(landlord.ApplicationUser);
        if (!updateResult.Succeeded)
        {
            return Result.Failure(updateResult.Errors.Select(error => error.Description));
        }

        if (!string.IsNullOrWhiteSpace(landlord.ApplicationUser.Email))
        {
            await _emailService.SendEmailAsync(
                landlord.ApplicationUser.Email!,
                "Your landlord account has been reactivated",
                "Your Sakanak landlord account has been reactivated by an administrator.");
        }

        return Result.Success();
    }

    public async Task<Result<PagedResult<AdminApartmentListItemDto>>> GetAllApartmentsAsync(
        int pageNumber = 1,
        int pageSize = 15,
        string? city = null,
        string? landlord = null,
        string? status = null,
        string sortBy = "address",
        bool descending = false)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _unitOfWork.Apartments.GetApartmentsWithDetailsQuery().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(city))
        {
            var normalizedCity = city.Trim().ToLower();
            query = query.Where(apartment => apartment.City.ToLower().Contains(normalizedCity));
        }

        if (!string.IsNullOrWhiteSpace(landlord))
        {
            var normalizedLandlord = landlord.Trim().ToLower();
            query = query.Where(apartment =>
                apartment.Landlord.ApplicationUser.Name.ToLower().Contains(normalizedLandlord) ||
                (apartment.Landlord.ApplicationUser.Email ?? string.Empty).ToLower().Contains(normalizedLandlord));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLowerInvariant();
            query = normalizedStatus switch
            {
                "active" => query.Where(apartment => apartment.IsActive),
                "inactive" => query.Where(apartment => !apartment.IsActive),
                _ => query
            };
        }

        query = (sortBy.ToLowerInvariant(), descending) switch
        {
            ("city", false) => query.OrderBy(apartment => apartment.City),
            ("city", true) => query.OrderByDescending(apartment => apartment.City),
            ("price", false) => query.OrderBy(apartment => apartment.PricePerMonth),
            ("price", true) => query.OrderByDescending(apartment => apartment.PricePerMonth),
            ("landlord", false) => query.OrderBy(apartment => apartment.Landlord.ApplicationUser.Name),
            ("landlord", true) => query.OrderByDescending(apartment => apartment.Landlord.ApplicationUser.Name),
            ("status", false) => query.OrderBy(apartment => apartment.IsActive),
            ("status", true) => query.OrderByDescending(apartment => apartment.IsActive),
            ("address", true) => query.OrderByDescending(apartment => apartment.Address),
            _ => query.OrderBy(apartment => apartment.Address)
        };

        var totalCount = await query.CountAsync();
        var apartments = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = apartments.Select(apartment => new AdminApartmentListItemDto
        {
            ApartmentId = apartment.ApartmentId,
            LandlordId = apartment.LandlordId,
            Address = apartment.Address,
            City = apartment.City,
            PricePerMonth = apartment.PricePerMonth,
            TotalSeats = apartment.TotalSeats,
            IsActive = apartment.IsActive,
            LandlordName = apartment.Landlord.ApplicationUser.Name,
            LandlordEmail = apartment.Landlord.ApplicationUser.Email ?? string.Empty,
            LatestRequestStatus = apartment.Requests.OrderByDescending(request => request.CreatedAt).Select(request => request.Status.ToString()).FirstOrDefault() ?? "No Request",
            PrimaryPhotoUrl = apartment.Media.Select(media => media.Url).FirstOrDefault()
        }).ToList();

        return Result<PagedResult<AdminApartmentListItemDto>>.Success(new PagedResult<AdminApartmentListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    public async Task<Result<AdminApartmentDetailsDto>> GetApartmentDetailsAsync(int apartmentId)
    {
        var apartment = await _unitOfWork.Apartments.GetApartmentsWithDetailsQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ApartmentId == apartmentId);

        if (apartment is null)
        {
            return Result<AdminApartmentDetailsDto>.Failure("Apartment not found.");
        }

        var dto = new AdminApartmentDetailsDto
        {
            ApartmentId = apartment.ApartmentId,
            LandlordId = apartment.LandlordId,
            Address = apartment.Address,
            City = apartment.City,
            PricePerMonth = apartment.PricePerMonth,
            TotalSeats = apartment.TotalSeats,
            Amenities = apartment.Amenities.ToList(),
            VirtualTourUrl = apartment.VirtualTourUrl,
            IsActive = apartment.IsActive,
            LatestRequestStatus = apartment.Requests.OrderByDescending(request => request.CreatedAt).Select(request => request.Status.ToString()).FirstOrDefault() ?? "No Request",
            LandlordName = apartment.Landlord.ApplicationUser.Name,
            LandlordEmail = apartment.Landlord.ApplicationUser.Email ?? string.Empty,
            LandlordPhoneNumber = apartment.Landlord.ApplicationUser.PhoneNumber,
            Photos = _mapper.Map<List<ApartmentMediaDto>>(apartment.Media)
        };

        return Result<AdminApartmentDetailsDto>.Success(dto);
    }

    public async Task<Result> SuspendApartmentAsync(int apartmentId, Guid adminApplicationUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure("Suspension reason is required.");
        }

        var admin = await _unitOfWork.Admins.GetByUserIdAsync(adminApplicationUserId);
        if (admin is null)
        {
            return Result.Failure("Admin profile was not found.");
        }

        var apartment = await _unitOfWork.Apartments.GetApartmentsWithDetailsQuery()
            .FirstOrDefaultAsync(item => item.ApartmentId == apartmentId);

        if (apartment is null)
        {
            return Result.Failure("Apartment not found.");
        }

        apartment.IsActive = false;
        
        var latestRequest = apartment.Requests.OrderByDescending(r => r.CreatedAt).FirstOrDefault();
        if (latestRequest != null)
        {
            latestRequest.Status = RequestStatus.Rejected;
            latestRequest.Message = $"Suspended by admin. Reason: {reason.Trim()}";
            latestRequest.ReviewedByAdminId = admin.AdminId;
            latestRequest.ResolvedAt = DateTime.UtcNow;
            await _unitOfWork.Requests.UpdateAsync(latestRequest);
        }

        await _unitOfWork.Apartments.UpdateAsync(apartment);
        await _unitOfWork.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(apartment.Landlord.ApplicationUser.Email))
        {
            await _emailService.SendEmailAsync(
                apartment.Landlord.ApplicationUser.Email!,
                "Your apartment listing has been suspended",
                $"Your apartment at {apartment.Address} has been suspended by an administrator. Reason: {reason.Trim()}");
        }

        return Result.Success();
    }

    public async Task<Result> ReactivateApartmentAsync(int apartmentId, Guid adminApplicationUserId)
    {
        var admin = await _unitOfWork.Admins.GetByUserIdAsync(adminApplicationUserId);
        if (admin is null)
        {
            return Result.Failure("Admin profile was not found.");
        }

        var apartment = await _unitOfWork.Apartments.GetApartmentsWithDetailsQuery()
            .FirstOrDefaultAsync(item => item.ApartmentId == apartmentId);

        if (apartment is null)
        {
            return Result.Failure("Apartment not found.");
        }

        var latestRequest = apartment.Requests.OrderByDescending(request => request.CreatedAt).FirstOrDefault();
        if (latestRequest != null)
        {
            latestRequest.Status = RequestStatus.Approved;
            latestRequest.Message = "Listing reactivated by administrator.";
            latestRequest.ReviewedByAdminId = admin.AdminId;
            latestRequest.ResolvedAt = DateTime.UtcNow;
            await _unitOfWork.Requests.UpdateAsync(latestRequest);
        }

        apartment.IsActive = true;
        await _unitOfWork.Apartments.UpdateAsync(apartment);
        await _unitOfWork.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(apartment.Landlord.ApplicationUser.Email))
        {
            await _emailService.SendEmailAsync(
                apartment.Landlord.ApplicationUser.Email!,
                "Your apartment listing has been reactivated",
                $"Your apartment at {apartment.Address} has been reactivated by an administrator.");
        }

        return Result.Success();
    }

    public async Task<Result<int>> GetPendingLandlordVerificationsCountAsync()
    {
        var count = await _unitOfWork.Landlords.GetLandlordsWithDetailsQuery()
            .AsNoTracking()
            .CountAsync(landlord => !landlord.VerificationStatus && landlord.VerificationRequestedAt != null && landlord.RejectionReason == null);
        return Result<int>.Success(count);
    }

    public async Task<Result<AdminDashboardStatsDto>> GetDashboardStatsAsync()
    {
        var pendingRequestsCount = await _unitOfWork.Requests.GetRequestsWithDetailsQuery()
            .AsNoTracking()
            .CountAsync(request => request.Status == RequestStatus.Pending);

        var historyQuery = _unitOfWork.Requests.GetRequestsWithDetailsQuery()
            .AsNoTracking()
            .Where(request => request.Status == RequestStatus.Approved || request.Status == RequestStatus.Rejected);
        
        var totalReviewedRequests = await historyQuery.CountAsync();
        var approvedRequestsCount = await historyQuery.CountAsync(request => request.Status == RequestStatus.Approved);
        var rejectedRequestsCount = await historyQuery.CountAsync(request => request.Status == RequestStatus.Rejected);

        var landlordsQuery = _unitOfWork.Landlords.GetLandlordsWithDetailsQuery()
            .AsNoTracking();

        var totalLandlords = await landlordsQuery.CountAsync();
        var suspendedLandlords = await landlordsQuery.CountAsync(landlord => landlord.ApplicationUser.Status == UserStatus.Suspended);

        var totalApartments = await _unitOfWork.Apartments.GetApartmentsWithDetailsQuery()
            .AsNoTracking()
            .CountAsync();

        var pendingLandlordVerificationsCount = await _unitOfWork.Landlords.GetLandlordsWithDetailsQuery()
            .AsNoTracking()
            .CountAsync(landlord => !landlord.VerificationStatus && landlord.VerificationRequestedAt != null && landlord.RejectionReason == null);

        var dto = new AdminDashboardStatsDto
        {
            PendingRequestsCount = pendingRequestsCount,
            TotalReviewedRequests = totalReviewedRequests,
            ApprovedRequestsCount = approvedRequestsCount,
            RejectedRequestsCount = rejectedRequestsCount,
            TotalLandlords = totalLandlords,
            SuspendedLandlords = suspendedLandlords,
            TotalApartments = totalApartments,
            PendingLandlordVerificationsCount = pendingLandlordVerificationsCount
        };

        return Result<AdminDashboardStatsDto>.Success(dto);
    }
}
