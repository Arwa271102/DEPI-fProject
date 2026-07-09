namespace Sakanak.BLL.DTOs.Auth;

public class ExternalRegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    
    // Profile Data
    public string? PhoneNumber { get; set; }
    public int Age { get; set; }
    public string? University { get; set; }
    public string? Faculty { get; set; }
}
