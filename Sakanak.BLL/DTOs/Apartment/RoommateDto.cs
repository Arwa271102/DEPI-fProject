namespace Sakanak.BLL.DTOs.Apartment;

public class RoommateDto
{
    public int StudentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string University { get; set; } = string.Empty;
    public string Faculty { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public decimal CompatibilityScore { get; set; }
    public string LifestyleSummary { get; set; } = string.Empty;
}
