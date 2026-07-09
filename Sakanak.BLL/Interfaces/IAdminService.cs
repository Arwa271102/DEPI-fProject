using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Admin;
using Sakanak.BLL.DTOs.Common;

namespace Sakanak.BLL.Interfaces;

public interface IAdminService
{
    Task<Result<PagedResult<LandlordListItemDto>>> GetAllLandlordsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? search = null,
        string? status = null,
        string sortBy = "registered",
        bool descending = true);

    Task<Result<LandlordDetailsDto>> GetLandlordDetailsAsync(int landlordId);
    Task<Result> SuspendLandlordAsync(int landlordId, Guid adminApplicationUserId, string reason);
    Task<Result> ReactivateLandlordAsync(int landlordId, Guid adminApplicationUserId);
    Task<Result<PagedResult<LandlordVerificationRequestDto>>> GetPendingVerificationsAsync(int pageNumber = 1, int pageSize = 10);
    Task<Result<PagedResult<LandlordVerificationRequestDto>>> GetAllVerificationsAsync(int pageNumber = 1, int pageSize = 10, string? status = null);
    Task<Result<LandlordVerificationRequestDto>> GetLandlordVerificationAsync(int landlordId);
    Task<Result> ApproveLandlordAsync(int landlordId, Guid adminApplicationUserId);
    Task<Result> RejectLandlordAsync(int landlordId, Guid adminApplicationUserId, string reason);

    Task<Result<PagedResult<AdminApartmentListItemDto>>> GetAllApartmentsAsync(
        int pageNumber = 1,
        int pageSize = 15,
        string? city = null,
        string? landlord = null,
        string? status = null,
        string sortBy = "address",
        bool descending = false);

    Task<Result<AdminApartmentDetailsDto>> GetApartmentDetailsAsync(int apartmentId);
    Task<Result> SuspendApartmentAsync(int apartmentId, Guid adminApplicationUserId, string reason);
    Task<Result> ReactivateApartmentAsync(int apartmentId, Guid adminApplicationUserId);
    Task<Result<int>> GetPendingLandlordVerificationsCountAsync();
    Task<Result<AdminDashboardStatsDto>> GetDashboardStatsAsync();
}
