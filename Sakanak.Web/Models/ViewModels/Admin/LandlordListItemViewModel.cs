namespace Sakanak.Web.Models.ViewModels.Admin;

public class LandlordListItemViewModel
{
    public int LandlordId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool VerificationStatus { get; set; }
    public int TotalApartments { get; set; }
    public int ActiveApartments { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
}
