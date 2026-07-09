namespace Sakanak.Web.Models.ViewModels.Shared;

public class PaginationViewModel
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Controller { get; set; }
    public Dictionary<string, string?> RouteValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int StartItem => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
    public int EndItem => Math.Min(PageNumber * PageSize, TotalCount);
}
