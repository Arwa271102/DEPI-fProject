namespace Sakanak.Web.Models.ViewModels.Admin;

public class LandlordDetailsViewModel
{
    public int LandlordId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int Age { get; set; }
    public bool VerificationStatus { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public int TotalApartments { get; set; }
    public int ActiveApartments { get; set; }
    public IReadOnlyList<LandlordApartmentViewModel> Apartments { get; set; } = Array.Empty<LandlordApartmentViewModel>();
    public SuspendLandlordViewModel SuspendLandlord { get; set; } = new();
}
