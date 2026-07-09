namespace Sakanak.Web.Models.ViewModels.Landlord;

public class RequestListItemViewModel
{
    public int RequestId { get; set; }
    public int ApartmentId { get; set; }
    public string ApartmentAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string LandlordName { get; set; } = string.Empty;
    public string LandlordEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ReviewedByAdminName { get; set; }
    public string? ThumbnailUrl { get; set; }
}
