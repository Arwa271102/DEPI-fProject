namespace Sakanak.Domain.Entities;

public class Request
{
    public int RequestId { get; set; }
    public int LandlordId { get; set; }
    public int ApartmentId { get; set; }
    public int? ReviewedByAdminId { get; set; }
    public Enums.RequestStatus Status { get; set; }
    public Enums.RequestType Type { get; set; } = Enums.RequestType.ApartmentUpload;
    public string Message { get; set; } = string.Empty;
    public string? PreviousValues { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Landlord Landlord { get; set; } = null!;
    public Apartment Apartment { get; set; } = null!;
    public Admin? ReviewedByAdmin { get; set; }
}
