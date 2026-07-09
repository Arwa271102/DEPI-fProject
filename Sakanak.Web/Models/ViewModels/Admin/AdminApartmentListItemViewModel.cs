namespace Sakanak.Web.Models.ViewModels.Admin;

public class AdminApartmentListItemViewModel
{
    public int ApartmentId { get; set; }
    public int LandlordId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public int TotalSeats { get; set; }
    public bool IsActive { get; set; }
    public string LandlordName { get; set; } = string.Empty;
    public string LandlordEmail { get; set; } = string.Empty;
    public string LatestRequestStatus { get; set; } = string.Empty;
    public string? PrimaryPhotoUrl { get; set; }
}
