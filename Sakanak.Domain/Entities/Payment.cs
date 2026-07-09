using System.ComponentModel.DataAnnotations.Schema;
using Sakanak.Domain.Enums;

namespace Sakanak.Domain.Entities;

public class Payment
{
    public int PaymentId { get; set; }
    public int StudentId { get; set; }
    public int LandlordId { get; set; }
    public int ApartmentId { get; set; }
    public int ContractId { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? StripeSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public PaymentStatus Status { get; set; }
    [NotMapped]
    public bool IsLate => DateTime.UtcNow > DueDate && Status != PaymentStatus.Paid;
    public Student Student { get; set; } = null!;
    public Landlord Landlord { get; set; } = null!;
    public Apartment Apartment { get; set; } = null!;
    public Contract Contract { get; set; } = null!;
}
