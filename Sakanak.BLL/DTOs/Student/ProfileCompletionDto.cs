namespace Sakanak.BLL.DTOs.Student;

public class ProfileCompletionDto
{
    public string Status { get; set; } = "Incomplete";
    public int CompletionPercentage { get; set; }
    public IReadOnlyList<string> MissingFields { get; set; } = Array.Empty<string>();
}
