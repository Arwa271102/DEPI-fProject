using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Common;
using Sakanak.BLL.DTOs.Request;

namespace Sakanak.BLL.Interfaces;

public interface IRequestService
{
    Task<Result<PagedResult<RequestListItemDto>>> GetLandlordRequestsAsync(
        Guid applicationUserId,
        int pageNumber = 1,
        int pageSize = 10,
        string? status = null,
        string sortBy = "createdat",
        bool descending = true);
    Task<Result<RequestDetailsDto>> GetRequestDetailsAsync(int requestId, Guid applicationUserId, bool requireAdmin = false);
    Task<Result> CancelPendingRequestAsync(int requestId, Guid applicationUserId);
    Task<Result<PagedResult<RequestListItemDto>>> GetPendingRequestsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? city = null,
        string? landlord = null,
        string sortBy = "createdat",
        bool descending = false);
    Task<Result<PagedResult<RequestListItemDto>>> GetRequestHistoryAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? status = null,
        string? city = null,
        string? landlord = null,
        string sortBy = "resolvedat",
        bool descending = true);
    Task<Result> ApproveRequestAsync(int requestId, Guid adminApplicationUserId);
    Task<Result> RejectRequestAsync(AdminReviewRequestDto dto, Guid adminApplicationUserId);
}
