using Sakanak.Web.Models.ViewModels.Shared;

namespace Sakanak.Web.Models.ViewModels.Landlord;

public class MyRequestsViewModel
{
    public IReadOnlyList<RequestListItemViewModel> Requests { get; set; } = Array.Empty<RequestListItemViewModel>();
    public PaginationViewModel Pagination { get; set; } = new();
    public string? StatusFilter { get; set; }
    public string SortBy { get; set; } = "createdat";
    public bool Descending { get; set; } = true;
}
