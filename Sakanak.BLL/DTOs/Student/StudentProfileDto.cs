namespace Sakanak.BLL.DTOs.Student;

public class StudentProfileDto
{
    public int StudentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string University { get; set; } = string.Empty;
    public string Faculty { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public bool QuestionnaireComplete { get; set; }
    public ProfileCompletionDto Completion { get; set; } = new();
}
