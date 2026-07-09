using Sakanak.BLL.DTOs.Apartment;

namespace Sakanak.BLL.DTOs.Admin;

public class LandlordVerificationRequestDto
{
    public int LandlordId { get; set; }
    public string LandlordName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? VerificationRequestedAt { get; set; }
    public string Status { get; set; } = "Pending";
    public string? RejectionReason { get; set; }
    public IReadOnlyList<ApartmentMediaDto> Documents { get; set; } = Array.Empty<ApartmentMediaDto>();
}
