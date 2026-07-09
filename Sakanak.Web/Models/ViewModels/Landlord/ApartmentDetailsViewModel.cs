namespace Sakanak.Web.Models.ViewModels.Landlord;

public class ApartmentDetailsViewModel
{
    public int ApartmentId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public int TotalSeats { get; set; }
    public int OccupiedSeats { get; set; }
    public int AvailableSeats { get; set; }
    public int ActiveBookingCount { get; set; }
    public IReadOnlyList<string> Amenities { get; set; } = Array.Empty<string>();
    public string? VirtualTourUrl { get; set; }
    public bool IsActive { get; set; }
    public string LatestRequestStatus { get; set; } = string.Empty;
    public string? LatestRequestMessage { get; set; }
    public DateTime? LatestRequestDate { get; set; }
    public DateTime? LatestResolvedAt { get; set; }
    public IReadOnlyList<ApartmentPhotoViewModel> Photos { get; set; } = Array.Empty<ApartmentPhotoViewModel>();
}
