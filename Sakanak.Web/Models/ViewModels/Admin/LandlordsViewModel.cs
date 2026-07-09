using Sakanak.Web.Models.ViewModels.Shared;

namespace Sakanak.Web.Models.ViewModels.Admin;

public class LandlordsViewModel
{
    public IReadOnlyList<LandlordListItemViewModel> Landlords { get; set; } = Array.Empty<LandlordListItemViewModel>();
    public PaginationViewModel Pagination { get; set; } = new();
    public string? Search { get; set; }
    public string? StatusFilter { get; set; }
    public string SortBy { get; set; } = "registered";
    public bool Descending { get; set; } = true;
}
