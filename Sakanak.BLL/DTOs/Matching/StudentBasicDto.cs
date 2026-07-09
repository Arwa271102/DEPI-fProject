namespace Sakanak.BLL.DTOs.Matching;

public class StudentBasicDto
{
    public int StudentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string University { get; set; } = string.Empty;
    public string Faculty { get; set; } = string.Empty;
    public string? ProfilePhotoUrl { get; set; }
    public decimal CompatibilityScore { get; set; }
    public string LifestyleSummary { get; set; } = string.Empty;
}
