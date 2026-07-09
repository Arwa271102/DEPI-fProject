namespace Sakanak.Domain.Entities;

public class Student
{
    public int StudentId { get; set; }
    public Guid ApplicationUserId { get; set; }
    public string University { get; set; } = string.Empty;
    public string Faculty { get; set; } = string.Empty;
    public int Age { get; set; }
    public int LatePaymentCount { get; set; }
    public int? ApartmentGroupId { get; set; }
    public ApplicationUser ApplicationUser { get; set; } = null!;
    public ApartmentGroup? ApartmentGroup { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public LifestyleQuestionnaire? Questionnaire { get; set; }
    public ICollection<Media> Media { get; set; } = new List<Media>();
}
