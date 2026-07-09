namespace Sakanak.Web.Models.ViewModels.Admin;

public class PendingRequestListItemViewModel
{
    public int RequestId { get; set; }
    public int ApartmentId { get; set; }
    public string ApartmentAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string LandlordName { get; set; } = string.Empty;
    public string LandlordEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
