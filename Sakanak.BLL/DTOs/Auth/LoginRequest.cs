namespace Sakanak.BLL.DTOs.Auth;

/// <summary>
/// Carries login credentials from the presentation layer into the BLL.
/// </summary>
public sealed class LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public bool RememberMe { get; init; }
}
