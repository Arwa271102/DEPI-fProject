using Sakanak.BLL.DTOs.Apartment;

namespace Sakanak.BLL.DTOs.Request;

public class RequestDetailsDto
{
    public int RequestId { get; set; }
    public int ApartmentId { get; set; }
    public int LandlordId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? PreviousValues { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string LandlordName { get; set; } = string.Empty;
    public string LandlordEmail { get; set; } = string.Empty;
    public string? LandlordPhoneNumber { get; set; }
    public string? ReviewedByAdminName { get; set; }
    public ApartmentDetailsDto Apartment { get; set; } = new();
}
