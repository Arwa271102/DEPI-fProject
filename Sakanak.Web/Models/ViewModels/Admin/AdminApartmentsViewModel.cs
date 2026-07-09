using Sakanak.Web.Models.ViewModels.Shared;

namespace Sakanak.Web.Models.ViewModels.Admin;

public class AdminApartmentsViewModel
{
    public IReadOnlyList<AdminApartmentListItemViewModel> Apartments { get; set; } = Array.Empty<AdminApartmentListItemViewModel>();
    public PaginationViewModel Pagination { get; set; } = new();
    public string? CityFilter { get; set; }
    public string? LandlordFilter { get; set; }
    public string? StatusFilter { get; set; }
    public string SortBy { get; set; } = "address";
    public bool Descending { get; set; }
}
