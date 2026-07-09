using Sakanak.Web.Models.ViewModels.Shared;

namespace Sakanak.Web.Models.ViewModels.Admin;

public class RequestHistoryViewModel
{
    public IReadOnlyList<PendingRequestListItemViewModel> Requests { get; set; } = Array.Empty<PendingRequestListItemViewModel>();
    public PaginationViewModel Pagination { get; set; } = new();
    public string? StatusFilter { get; set; }
    public string? CityFilter { get; set; }
    public string? LandlordFilter { get; set; }
    public string SortBy { get; set; } = "resolvedat";
    public bool Descending { get; set; } = true;
}
