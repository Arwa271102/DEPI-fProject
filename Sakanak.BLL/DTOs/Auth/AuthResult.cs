namespace Sakanak.BLL.DTOs.Auth;

/// <summary>
/// Represents the outcome of an authentication or registration operation.
/// </summary>
public sealed class AuthResult
{
    public bool Succeeded { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// The primary role of the authenticated / registered user (e.g. "Student").
    /// Used by controllers to determine which dashboard to redirect to.
    /// </summary>
    public string? Role { get; private init; }
    public string? Code { get; private init; }
    public string? UserId { get; private init; }
    public bool RequiresPasswordSet { get; private init; }

    // External Login Data
    public string? ExternalEmail { get; private init; }
    public string? ExternalName { get; private init; }
    public string? LoginProvider { get; private init; }
    public string? ProviderKey { get; private init; }

    public static AuthResult Ok(string? role = null) =>
        new() { Succeeded = true, Role = role };

    public static AuthResult Fail(params string[] errors) =>
        new() { Succeeded = false, Errors = errors };

    public static AuthResult FailWithCode(string code, params string[] errors) =>
        new() { Succeeded = false, Code = code, Errors = errors };

    public static AuthResult RequiresTwoFactor(string userId) =>
        new() { Succeeded = false, Code = "RequiresTwoFactor", UserId = userId };

    public static AuthResult ExternalRegistrationRequired(string email, string name, string provider, string key) =>
        new() { Succeeded = false, Code = "RequiresExternalRegistration", ExternalEmail = email, ExternalName = name, LoginProvider = provider, ProviderKey = key };

    public static AuthResult NeedPasswordSet(string? role = null) =>
        new() { Succeeded = true, RequiresPasswordSet = true, Role = role };

    public static AuthResult Fail(IEnumerable<string> errors) =>
        new() { Succeeded = false, Errors = [.. errors] };
}
