namespace Sakanak.Web.Models.ViewModels.Admin;

public class LandlordApartmentViewModel
{
    public int ApartmentId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public bool IsActive { get; set; }
    public string RequestStatus { get; set; } = string.Empty;
}
