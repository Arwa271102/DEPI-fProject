using Sakanak.BLL.DTOs.Search;
using Sakanak.Web.Models.ViewModels.Shared;

namespace Sakanak.Web.Models.ViewModels.Student;

public class ApartmentSearchViewModel
{
    public ApartmentSearchDto Search { get; set; } = new();
    public IReadOnlyList<ApartmentListItemDto> Apartments { get; set; } = Array.Empty<ApartmentListItemDto>();
    public PaginationViewModel Pagination { get; set; } = new();
    public IReadOnlyList<string> AvailableCities { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> AvailableAmenities { get; set; } = Array.Empty<string>();
    public int TotalCount { get; set; }
}
