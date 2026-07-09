using Microsoft.AspNetCore.Identity;
using Sakanak.Domain.Enums;

namespace Sakanak.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public Student? StudentProfile { get; set; }
    public Landlord? LandlordProfile { get; set; }
    public Admin? AdminProfile { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsProfileComplete { get; set; }
}
