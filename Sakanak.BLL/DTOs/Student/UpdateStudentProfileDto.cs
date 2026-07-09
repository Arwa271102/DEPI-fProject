using Microsoft.AspNetCore.Http;

namespace Sakanak.BLL.DTOs.Student;

public class UpdateStudentProfileDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string University { get; set; } = string.Empty;
    public string Faculty { get; set; } = string.Empty;
    public IFormFile? ProfilePhoto { get; set; }
}
