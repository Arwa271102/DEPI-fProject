namespace Sakanak.BLL.DTOs.Auth;

/// <summary>
/// Carries registration data from the presentation layer into the BLL.
/// </summary>
public sealed class RegisterRequest
{
    public string Name { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public int Age { get; init; }

    // Student-specific (required when Role == "Student")
    public string? University { get; init; }
    public string? Faculty { get; init; }

    public string Password { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
}
