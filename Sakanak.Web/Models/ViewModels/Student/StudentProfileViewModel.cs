using Microsoft.AspNetCore.Http;

namespace Sakanak.Web.Models.ViewModels.Student;

public class StudentProfileViewModel
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
    public string CompletionStatus { get; set; } = "Incomplete";
    public int CompletionPercentage { get; set; }
    public IReadOnlyList<string> MissingFields { get; set; } = Array.Empty<string>();
    public IFormFile? ProfilePhoto { get; set; }
}
