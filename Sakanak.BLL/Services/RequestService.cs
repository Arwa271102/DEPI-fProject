using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Common;
using Sakanak.BLL.DTOs.Request;
using Sakanak.BLL.Interfaces;
using Sakanak.DAL.Data;
using Sakanak.DAL.UnitOfWork;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Services;

/// <summary>
/// Manages apartment approval requests for landlords and admins.
/// </summary>
public class RequestService : IRequestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly IValidator<AdminReviewRequestDto> _reviewValidator;
    private readonly INotificationService _notificationService;

    public RequestService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IEmailService emailService,
        IValidator<AdminReviewRequestDto> reviewValidator,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _reviewValidator = reviewValidator;
        _notificationService = notificationService;
    }

    public async Task<Result<PagedResult<RequestListItemDto>>> GetLandlordRequestsAsync(
        Guid applicationUserId,
        int pageNumber = 1,
        int pageSize = 10,
        string? status = null,
        string sortBy = "createdat",
        bool descending = true)
    {
        var landlord = await _unitOfWork.Landlords.GetByUserIdAsync(applicationUserId);
        if (landlord is null)
        {
            return Result<PagedResult<RequestListItemDto>>.Failure("Landlord profile was not found.");
        }

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = BuildRequestQuery()
            .Where(request => request.LandlordId == landlord.LandlordId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<RequestStatus>(status.Trim(), true, out var parsedStatus))
            {
                query = query.Where(request => request.Status == parsedStatus);
            }
        }

        query = ApplyRequestSorting(query, sortBy, descending);

        var totalCount = await query.CountAsync();
        var requests = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Result<PagedResult<RequestListItemDto>>.Success(new PagedResult<RequestListItemDto>
        {
            Items = _mapper.Map<List<RequestListItemDto>>(requests),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    public async Task<Result<RequestDetailsDto>> GetRequestDetailsAsync(int requestId, Guid applicationUserId, bool requireAdmin = false)
    {
        var request = await BuildRequestQuery()
            .FirstOrDefaultAsync(item => item.RequestId == requestId);

        if (request is null)
        {
            return Result<RequestDetailsDto>.Failure("Request not found.");
        }

        if (!requireAdmin)
        {
            var landlord = await _unitOfWork.Landlords.GetByUserIdAsync(applicationUserId);
            if (landlord is null || landlord.LandlordId != request.LandlordId)
            {
                return Result<RequestDetailsDto>.Failure("You do not have access to this request.");
            }
        }

        var dto = _mapper.Map<RequestDetailsDto>(request);
        dto.Apartment = _mapper.Map<Sakanak.BLL.DTOs.Apartment.ApartmentDetailsDto>(request.Apartment);
        return Result<RequestDetailsDto>.Success(dto);
    }

    public async Task<Result> CancelPendingRequestAsync(int requestId, Guid applicationUserId)
    {
        var landlord = await _unitOfWork.Landlords.GetByUserIdAsync(applicationUserId);
        if (landlord is null)
        {
            return Result.Failure("Landlord profile was not found.");
        }

        var request = await _unitOfWork.Requests.GetRequestsWithDetailsQuery()
            .FirstOrDefaultAsync(item => item.RequestId == requestId && item.LandlordId == landlord.LandlordId);
        if (request is null)
        {
            return Result.Failure("Request not found.");
        }

        if (request.Status != RequestStatus.Pending)
        {
            return Result.Failure("Only pending requests can be cancelled.");
        }

        request.Status = RequestStatus.Cancelled;
        request.Message = "Request cancelled by landlord.";
        request.ResolvedAt = DateTime.UtcNow;

        var apartment = await _unitOfWork.Apartments.GetByIdAsync(request.ApartmentId);
        if (apartment is not null)
        {
            apartment.IsActive = false;
            await _unitOfWork.Apartments.UpdateAsync(apartment);
        }

        await _unitOfWork.Requests.UpdateAsync(request);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<PagedResult<RequestListItemDto>>> GetPendingRequestsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? city = null,
        string? landlord = null,
        string sortBy = "createdat",
        bool descending = false)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = BuildRequestQuery().Where(request => request.Status == RequestStatus.Pending);

        if (!string.IsNullOrWhiteSpace(city))
        {
            var normalizedCity = city.Trim().ToLower();
            query = query.Where(request => request.Apartment.City.ToLower().Contains(normalizedCity));
        }

        if (!string.IsNullOrWhiteSpace(landlord))
        {
            var normalizedLandlord = landlord.Trim().ToLower();
            query = query.Where(request => request.Landlord.ApplicationUser.Name.ToLower().Contains(normalizedLandlord));
        }

        query = ApplyRequestSorting(query, sortBy, descending);

        var totalCount = await query.CountAsync();
        var requests = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Result<PagedResult<RequestListItemDto>>.Success(new PagedResult<RequestListItemDto>
        {
            Items = _mapper.Map<List<RequestListItemDto>>(requests),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    public async Task<Result<PagedResult<RequestListItemDto>>> GetRequestHistoryAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? status = null,
        string? city = null,
        string? landlord = null,
        string sortBy = "resolvedat",
        bool descending = true)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 50);

        // Return ALL requests (all statuses) for the admin history/full view
        var query = BuildRequestQuery();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<RequestStatus>(status.Trim(), true, out var parsedStatus))
            {
                query = query.Where(request => request.Status == parsedStatus);
            }
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var normalizedCity = city.Trim().ToLower();
            query = query.Where(request => request.Apartment.City.ToLower().Contains(normalizedCity));
        }

        if (!string.IsNullOrWhiteSpace(landlord))
        {
            var normalizedLandlord = landlord.Trim().ToLower();
            query = query.Where(request => request.Landlord.ApplicationUser.Name.ToLower().Contains(normalizedLandlord));
        }

        query = ApplyRequestSorting(query, sortBy, descending);

        var totalCount = await query.CountAsync();
        var requests = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Result<PagedResult<RequestListItemDto>>.Success(new PagedResult<RequestListItemDto>
        {
            Items = _mapper.Map<List<RequestListItemDto>>(requests),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    public async Task<Result> ApproveRequestAsync(int requestId, Guid adminApplicationUserId)
    {
        var admin = await _unitOfWork.Admins.GetByUserIdAsync(adminApplicationUserId);
        if (admin is null)
        {
            return Result.Failure("Admin profile was not found.");
        }

        var request = await _unitOfWork.Requests.GetRequestsWithDetailsQuery()
            .FirstOrDefaultAsync(item => item.RequestId == requestId);

        if (request is null)
        {
            return Result.Failure("Request not found.");
        }

        if (request.Status != RequestStatus.Pending)
        {
            return Result.Failure("This request has already been reviewed.");
        }

        request.Status = RequestStatus.Approved;
        request.Message = "Apartment approved by admin.";
        request.ReviewedByAdminId = admin.AdminId;
        request.ResolvedAt = DateTime.UtcNow;
        request.Apartment.IsActive = true;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.Requests.UpdateAsync(request);
            await _unitOfWork.Apartments.UpdateAsync(request.Apartment);
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        if (!string.IsNullOrWhiteSpace(request.Landlord.ApplicationUser.Email))
        {
            await _emailService.SendEmailAsync(
                request.Landlord.ApplicationUser.Email!,
                "Your apartment listing has been approved",
                $"Your apartment at {request.Apartment.Address} has been approved and is now visible on Sakanak.");
        }

        await _notificationService.CreateNotificationAsync(
            request.Landlord.ApplicationUserId,
            "Apartment Approved",
            $"Your apartment at {request.Apartment.Address} has been approved and is now live",
            NotificationType.ApartmentApproved,
            $"/Landlord/ApartmentDetails/{request.ApartmentId}");

        return Result.Success();
    }

    public async Task<Result> RejectRequestAsync(AdminReviewRequestDto dto, Guid adminApplicationUserId)
    {
        var validation = await _reviewValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Result.Failure(validation.Errors.Select(error => error.ErrorMessage));
        }

        var admin = await _unitOfWork.Admins.GetByUserIdAsync(adminApplicationUserId);
        if (admin is null)
        {
            return Result.Failure("Admin profile was not found.");
        }

        var request = await _unitOfWork.Requests.GetRequestsWithDetailsQuery()
            .FirstOrDefaultAsync(item => item.RequestId == dto.RequestId);

        if (request is null)
        {
            return Result.Failure("Request not found.");
        }

        if (request.Status != RequestStatus.Pending)
        {
            return Result.Failure("This request has already been reviewed.");
        }

        request.Status = RequestStatus.Rejected;
        request.Message = dto.Reason!.Trim();
        request.ReviewedByAdminId = admin.AdminId;
        request.ResolvedAt = DateTime.UtcNow;
        request.Apartment.IsActive = false;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.Requests.UpdateAsync(request);
            await _unitOfWork.Apartments.UpdateAsync(request.Apartment);
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        if (!string.IsNullOrWhiteSpace(request.Landlord.ApplicationUser.Email))
        {
            await _emailService.SendEmailAsync(
                request.Landlord.ApplicationUser.Email!,
                "Your apartment listing was rejected",
                $"Your apartment at {request.Apartment.Address} was rejected. Reason: {request.Message}");
        }

        await _notificationService.CreateNotificationAsync(
            request.Landlord.ApplicationUserId,
            "Apartment Request Rejected",
            $"Your apartment upload request was rejected. Reason: {request.Message}",
            NotificationType.ApartmentRejected,
            "/Landlord/MyRequests");

        return Result.Success();
    }

    private IQueryable<Sakanak.Domain.Entities.Request> BuildRequestQuery()
        => _unitOfWork.Requests.GetRequestsWithDetailsQuery().AsNoTracking();

    private static IQueryable<Sakanak.Domain.Entities.Request> ApplyRequestSorting(
        IQueryable<Sakanak.Domain.Entities.Request> query,
        string sortBy,
        bool descending)
    {
        return (sortBy.ToLowerInvariant(), descending) switch
        {
            ("apartment", false) => query.OrderBy(request => request.Apartment.Address),
            ("apartment", true) => query.OrderByDescending(request => request.Apartment.Address),
            ("city", false) => query.OrderBy(request => request.Apartment.City),
            ("city", true) => query.OrderByDescending(request => request.Apartment.City),
            ("landlord", false) => query.OrderBy(request => request.Landlord.ApplicationUser.Name),
            ("landlord", true) => query.OrderByDescending(request => request.Landlord.ApplicationUser.Name),
            ("status", false) => query.OrderBy(request => request.Status),
            ("status", true) => query.OrderByDescending(request => request.Status),
            ("resolvedat", false) => query.OrderBy(request => request.ResolvedAt ?? request.CreatedAt),
            ("resolvedat", true) => query.OrderByDescending(request => request.ResolvedAt ?? request.CreatedAt),
            ("createdat", false) => query.OrderBy(request => request.CreatedAt),
            ("createdat", true) => query.OrderByDescending(request => request.CreatedAt),
            _ => descending ? query.OrderByDescending(request => request.CreatedAt) : query.OrderBy(request => request.CreatedAt)
        };
    }
}
