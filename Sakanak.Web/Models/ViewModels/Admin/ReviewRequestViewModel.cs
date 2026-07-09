using Sakanak.Web.Models.ViewModels.Landlord;

namespace Sakanak.Web.Models.ViewModels.Admin;

public class ReviewRequestViewModel
{
    public int RequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string LandlordName { get; set; } = string.Empty;
    public string LandlordEmail { get; set; } = string.Empty;
    public string? LandlordPhoneNumber { get; set; }
    public ApartmentDetailsViewModel Apartment { get; set; } = new();
    public RejectRequestViewModel RejectRequest { get; set; } = new();
}
