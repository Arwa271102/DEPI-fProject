using Microsoft.AspNetCore.Authentication;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Auth;
using Sakanak.Domain.Entities;

namespace Sakanak.BLL.Interfaces;

/// <summary>
/// Defines the authentication and authorization contract for the Business Logic Layer.
/// Controllers must only interact with this interface — never with Identity or repositories directly.
/// </summary>
public interface IAuthService
{
    /// <summary>Validates credentials and signs the user in.</summary>
    Task<AuthResult> LoginAsync(LoginRequest request);

    /// <summary>
    /// Creates the Identity user, assigns the role, creates the domain profile
    /// (Student or Landlord), and signs the user in — all in a single transaction.
    /// </summary>
    Task<AuthResult> RegisterAsync(RegisterRequest request, string role);

    /// <summary>Signs the current user out.</summary>
    Task LogoutAsync();

    /// <summary>Confirms a user's email address using a token.</summary>
    Task<AuthResult> ConfirmEmailAsync(string userId, string token);

    /// <summary>Resends the confirmation email.</summary>
    Task<AuthResult> ResendConfirmationEmailAsync(string email, string baseUrl);

    /// <summary>Generates and sends a 2FA code via email.</summary>
    Task<AuthResult> SendTwoFactorCodeAsync(string userId);

    /// <summary>Verifies the 2FA code and completes login.</summary>
    Task<bool> VerifyTwoFactorCodeAsync(string userId, string code);

    /// <summary>Checks if two-factor authentication is enabled for a user.</summary>
    Task<bool> IsTwoFactorEnabledAsync(string userId);

    /// <summary>Enables two-factor authentication for a user.</summary>
    Task<AuthResult> EnableTwoFactorAsync(string userId);

    /// <summary>Disables two-factor authentication for a user after verifying an OTP.</summary>
    Task<AuthResult> DisableTwoFactorAsync(string userId, string code);

    /// <summary>Deactivates (soft deletes) a user account after verifying their password.</summary>
    Task<Result> DeactivateAccountAsync(string userId, string password);

    /// <summary>Generates a password reset token and sends it via email.</summary>
    Task<Result> GeneratePasswordResetTokenAsync(string email, string baseUrl);

    /// <summary>Resets the user's password using a valid token.</summary>
    Task<Result> ResetPasswordAsync(string userId, string token, string newPassword);

    /// <summary>Configures the external authentication properties for a provider, preserving the selected role.</summary>
    AuthenticationProperties ConfigureExternalAuthenticationProperties(string provider, string redirectUrl, string? role = null);

    /// <summary>Handles the unified external login callback (Login, Register, and Linking).</summary>
    Task<AuthResult> HandleExternalLoginCallbackAsync();

    /// <summary>Adds a password to an external-only account that currently has no local password.</summary>
    Task<Result> SetPasswordForExternalUserAsync(string userId, string password);

    /// <summary>
    /// Backward-compatible alias for adding password to passwordless users.
    /// Prefer <see cref="SetPasswordForExternalUserAsync"/> for new code.
    /// </summary>
    Task<Result> AddPasswordAsync(string userId, string password);

    /// <summary>Checks if a user's profile is complete.</summary>
    Task<bool> IsProfileCompleteAsync(ApplicationUser user);

    /// <summary>Retrieves initial data for the onboarding form.</summary>
    Task<(string? Role, int Age, string? PhoneNumber)> GetOnboardingDataAsync(string userId);

    /// <summary>Completes registration for a new external user (Google) in one step.</summary>
    Task<AuthResult> CompleteExternalRegistrationAsync(ExternalRegisterRequest request);

    /// <summary>Completes the user profile with missing information.</summary>
    Task<Result> CompleteProfileAsync(string userId, string? password, string university, string faculty, string phoneNumber, int age);
}
