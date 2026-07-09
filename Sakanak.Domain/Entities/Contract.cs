using Sakanak.Domain.Enums;

namespace Sakanak.Domain.Entities;

public class Contract
{
    public int ContractId { get; set; }
    public int BookingId { get; set; }
    public int StudentId { get; set; }
    public int ApartmentId { get; set; }
    public int LandlordId { get; set; }
    public int? VerifiedByAdminId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ContractStatus Status { get; set; }
    public string? DocumentUrl { get; set; }
    public string? RejectionReason { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancellationDate { get; set; }
    public int? CancelledByAdminId { get; set; }
    public Booking Booking { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public Apartment Apartment { get; set; } = null!;
    public Landlord Landlord { get; set; } = null!;
    public Admin? VerifiedByAdmin { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Media> Media { get; set; } = new List<Media>();
}
