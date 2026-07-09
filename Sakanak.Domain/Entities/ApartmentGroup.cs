using Sakanak.Domain.Enums;

namespace Sakanak.Domain.Entities;

public class ApartmentGroup
{
    public int GroupId { get; set; }
    public int ApartmentId { get; set; }
    public int MaxMembers { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public GroupStatus GroupStatus { get; set; }
    public Apartment Apartment { get; set; } = null!;
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
