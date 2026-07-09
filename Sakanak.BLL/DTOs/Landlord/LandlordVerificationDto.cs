using Sakanak.BLL.DTOs.Apartment;

namespace Sakanak.BLL.DTOs.Landlord;

public class LandlordVerificationDto
{
    public int LandlordId { get; set; }
    public string LandlordName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? VerificationRequestedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? RejectionReason { get; set; }
    public IReadOnlyList<ApartmentMediaDto> Documents { get; set; } = Array.Empty<ApartmentMediaDto>();
}
