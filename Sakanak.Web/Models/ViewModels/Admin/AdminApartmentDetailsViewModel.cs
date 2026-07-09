using Sakanak.Web.Models.ViewModels.Landlord;

namespace Sakanak.Web.Models.ViewModels.Admin;

public class AdminApartmentDetailsViewModel
{
    public int ApartmentId { get; set; }
    public int LandlordId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public int TotalSeats { get; set; }
    public IReadOnlyList<string> Amenities { get; set; } = Array.Empty<string>();
    public string? VirtualTourUrl { get; set; }
    public bool IsActive { get; set; }
    public string LatestRequestStatus { get; set; } = string.Empty;
    public string LandlordName { get; set; } = string.Empty;
    public string LandlordEmail { get; set; } = string.Empty;
    public string? LandlordPhoneNumber { get; set; }
    public IReadOnlyList<ApartmentPhotoViewModel> Photos { get; set; } = Array.Empty<ApartmentPhotoViewModel>();
    public SuspendApartmentViewModel SuspendApartment { get; set; } = new();
}
