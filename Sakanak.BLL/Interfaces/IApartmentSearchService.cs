using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Common;
using Sakanak.BLL.DTOs.Search;

namespace Sakanak.BLL.Interfaces;

public interface IApartmentSearchService
{
    Task<Result<PagedResult<ApartmentListItemDto>>> SearchApartmentsAsync(ApartmentSearchDto searchDto);
    Task<Result<ApartmentDetailDto>> GetApartmentDetailsAsync(int apartmentId, int studentId);
    Task<Result<List<string>>> GetAvailableCitiesAsync();
    Task<Result<List<string>>> GetAvailableAmenitiesAsync();
}
