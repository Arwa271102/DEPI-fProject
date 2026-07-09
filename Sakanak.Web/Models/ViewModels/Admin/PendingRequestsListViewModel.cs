using Sakanak.Web.Models.ViewModels.Shared;

namespace Sakanak.Web.Models.ViewModels.Admin;

public class PendingRequestsListViewModel
{
    public IReadOnlyList<PendingRequestListItemViewModel> Requests { get; set; } = Array.Empty<PendingRequestListItemViewModel>();
    public PaginationViewModel Pagination { get; set; } = new();
    public string? CityFilter { get; set; }
    public string? LandlordFilter { get; set; }
    public string SortBy { get; set; } = "createdat";
    public bool Descending { get; set; }
}
