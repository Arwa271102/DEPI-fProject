namespace Sakanak.Web.Models.ViewModels.Landlord;

public class ApartmentListItemViewModel
{
    public int ApartmentId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public int TotalSeats { get; set; }
    public int OccupiedSeats { get; set; }
    public int AvailableSeats { get; set; }
    public int ActiveBookingCount { get; set; }
    public bool IsActive { get; set; }
    public string ApartmentStatusDisplay { get; set; } = string.Empty;
    public string LatestRequestStatus { get; set; } = string.Empty;
    public DateTime? LatestRequestDate { get; set; }
    public string? PrimaryPhotoUrl { get; set; }
}
