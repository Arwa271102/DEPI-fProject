namespace Sakanak.Domain.Entities;

public class Landlord
{
    public int LandlordId { get; set; }
    public Guid ApplicationUserId { get; set; }
    public bool VerificationStatus { get; set; }
    public DateTime? VerificationRequestedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public int? VerifiedByAdminId { get; set; }
    public string? RejectionReason { get; set; }
    public int Age { get; set; }
    public int TotalProperties { get; set; }
    public ApplicationUser ApplicationUser { get; set; } = null!;
    public ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Media> Media { get; set; } = new List<Media>();
    public ICollection<Request> Requests { get; set; } = new List<Request>();
    public Admin? VerifiedByAdmin { get; set; }
}
