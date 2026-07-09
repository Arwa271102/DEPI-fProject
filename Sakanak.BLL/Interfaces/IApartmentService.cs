using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Apartment;
using Sakanak.BLL.DTOs.Common;

namespace Sakanak.BLL.Interfaces;

public interface IApartmentService
{
    Task<Result<int>> CreateApartmentAsync(CreateApartmentDto dto, Guid applicationUserId);
    Task<Result<PagedResult<ApartmentListItemDto>>> GetLandlordApartmentsAsync(
        Guid applicationUserId,
        int pageNumber = 1,
        int pageSize = 10,
        string? city = null,
        string? requestStatus = null,
        string sortBy = "created",
        bool descending = true);
    Task<Result<ApartmentDetailsDto>> GetApartmentByIdAsync(int apartmentId, Guid applicationUserId, bool requireOwnership = true);
    Task<Result> UpdateApartmentAsync(UpdateApartmentDto dto, Guid applicationUserId);
    Task<Result> ToggleApartmentActiveStatusAsync(int apartmentId, Guid applicationUserId);
    Task<Result> DeleteApartmentAsync(int apartmentId, Guid applicationUserId);
    Task<Result<bool>> ApartmentBelongsToLandlordAsync(int apartmentId, Guid applicationUserId);
}
