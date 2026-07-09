using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using System.Text;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Auth;
using Sakanak.BLL.Interfaces;
using Sakanak.DAL.UnitOfWork;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Services;

/// <summary>
/// Implements all authentication and authorization workflows.
/// Orchestrates Identity operations, UnitOfWork repository calls, and domain entity creation.
/// This is the single entry point for user-related operations consumed by controllers.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IUnitOfWork unitOfWork,
        IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    // ─── Login ───────────────────────────────────────────────────────────────

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || user.IsDeleted)
            return AuthResult.Fail("Your account has been deactivated or invalid login attempt.");

        if (user.Status == UserStatus.Suspended)
            return AuthResult.Fail("Your account has been suspended. Please contact support.");

        if (!user.EmailConfirmed)
            return AuthResult.FailWithCode("EmailNotConfirmed", "Please confirm your email before logging in.");

        if (!await _userManager.HasPasswordAsync(user))
        {
            return AuthResult.FailWithCode(
                "ExternalOnlyAccount",
                "This account uses Google login. Please sign in with Google or set a password.");
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded || result.RequiresTwoFactor)
        {
            if (result.RequiresTwoFactor)
            {
                await SendTwoFactorCodeInternalAsync(user);
                return AuthResult.RequiresTwoFactor(user.Id.ToString());
            }

            var roles = await _userManager.GetRolesAsync(user);
            return AuthResult.Ok(roles.FirstOrDefault());
        }

        if (result.IsLockedOut)
            return AuthResult.Fail("Account locked out.");

        return AuthResult.Fail("Invalid login attempt.");
    }

    // ─── Register ────────────────────────────────────────────────────────────

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, string role)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null && !existingUser.IsDeleted)
        {
            if (!existingUser.EmailConfirmed)
            {
                await ResendConfirmationEmailAsync(request.Email, request.BaseUrl);
                return AuthResult.FailWithCode("UserExistsNotConfirmed", "User exists but email is not confirmed.");
            }
            return AuthResult.FailWithCode("UserAlreadyExists", "User already exists with this email.");
        }

        var existingUserByName = await _userManager.FindByNameAsync(request.Username);
        if (existingUserByName != null)
        {
            return AuthResult.FailWithCode("UsernameTaken", "Username is already taken.");
        }

        var normalizedRole = NormalizeRole(role);
        if (normalizedRole is null)
            return AuthResult.Fail("Choose either Student or Landlord.");

        await EnsureRoleExistsAsync(normalizedRole);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            UserName = request.Username,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            RegistrationDate = DateTime.UtcNow,
            Status = UserStatus.Active,
            EmailConfirmed = false,
            IsProfileComplete = true   // All required data is collected during normal registration
        };

        await _unitOfWork.BeginTransactionAsync();

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return AuthResult.Fail(createResult.Errors.Select(e => e.Description));
        }

        if (string.Equals(normalizedRole, "Student", StringComparison.OrdinalIgnoreCase))
        {
            await _unitOfWork.Students.AddAsync(new Student
            {
                ApplicationUserId = user.Id,
                University = request.University?.Trim() ?? string.Empty,
                Faculty = request.Faculty?.Trim() ?? string.Empty,
                Age = request.Age
            });
        }
        else
        {
            await _unitOfWork.Landlords.AddAsync(new Landlord
            {
                ApplicationUserId = user.Id,
                VerificationStatus = false,
                TotalProperties = 0,
                Age = request.Age
            });
        }

        await _unitOfWork.SaveChangesAsync();

        var roleResult = await _userManager.AddToRoleAsync(user, normalizedRole);
        if (!roleResult.Succeeded)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return AuthResult.Fail(roleResult.Errors.Select(e => e.Description));
        }

        await _unitOfWork.CommitTransactionAsync();

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmationUrl = $"{request.BaseUrl.TrimEnd('/')}/Account/ConfirmEmail?userId={user.Id}&token={encodedToken}";
        
        var emailBody = $@"
            <h2>Welcome to Sakanak!</h2>
            <p>Please confirm your email by clicking the link below:</p>
            <p><a href='{confirmationUrl}'>Confirm My Email</a></p>";

        await _emailService.SendEmailAsync(user.Email!, "Confirm your email - Sakanak", emailBody);

        return AuthResult.Ok(normalizedRole);
    }

    // ─── Logout ──────────────────────────────────────────────────────────────

    public async Task LogoutAsync() => await _signInManager.SignOutAsync();

    // ─── Confirm Email ───────────────────────────────────────────────────────

    public async Task<AuthResult> ConfirmEmailAsync(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return AuthResult.Fail("User not found or deactivated.");

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
            return AuthResult.Fail(result.Errors.Select(e => e.Description));

        return AuthResult.Ok();
    }

    // ─── Resend Confirmation Email ───────────────────────────────────────────

    public async Task<AuthResult> ResendConfirmationEmailAsync(string email, string baseUrl)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || user.IsDeleted)
            return AuthResult.Ok();

        if (user.EmailConfirmed)
            return AuthResult.FailWithCode("UserAlreadyExists", "Account already Exists");

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmationUrl = $"{baseUrl.TrimEnd('/')}/Account/ConfirmEmail?userId={user.Id}&token={encodedToken}";
        
        var emailBody = $@"
            <h2>Welcome to Sakanak!</h2>
            <p>Please confirm your email by clicking the link below:</p>
            <p><a href='{confirmationUrl}'>Confirm My Email</a></p>";

        await _emailService.SendEmailAsync(user.Email!, "Confirm your email - Sakanak", emailBody);

        return AuthResult.Ok();
    }

    // ─── Two-Factor Authentication ───────────────────────────────────────────

    public async Task<AuthResult> SendTwoFactorCodeAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return AuthResult.Fail("User not found or deactivated.");

        await SendTwoFactorCodeInternalAsync(user);
        return AuthResult.Ok();
    }

    private async Task SendTwoFactorCodeInternalAsync(ApplicationUser user)
    {
        var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
        var message = $@"
            <h2>Your Two-Factor Authentication Code</h2>
            <p>Please enter the following 6-digit code to complete your request:</p>
            <h3 style='letter-spacing: 5px; color: #0d6efd;'>{code}</h3>
            <p><em>This code will expire in 5 minutes.</em></p>";

        await _emailService.SendEmailAsync(user.Email!, "Your 2FA Code - Sakanak", message);
    }

    public async Task<bool> VerifyTwoFactorCodeAsync(string userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return false;

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", code);
        if (isValid)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            return true;
        }

        return false;
    }

    public async Task<bool> IsTwoFactorEnabledAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.TwoFactorEnabled ?? false;
    }

    public async Task<AuthResult> EnableTwoFactorAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return AuthResult.Fail("User not found or deactivated.");

        var result = await _userManager.SetTwoFactorEnabledAsync(user, true);
        if (!result.Succeeded)
            return AuthResult.Fail(result.Errors.Select(e => e.Description));

        return AuthResult.Ok();
    }

    public async Task<AuthResult> DisableTwoFactorAsync(string userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return AuthResult.Fail("User not found or deactivated.");

        if (user.TwoFactorEnabled)
        {
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", code);
            if (!isValid)
                return AuthResult.Fail("Invalid or expired 2FA code.");
        }

        var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!result.Succeeded)
            return AuthResult.Fail(result.Errors.Select(e => e.Description));

        return AuthResult.Ok();
    }

    public async Task<Result> DeactivateAccountAsync(string userId, string password)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return Result.Failure("User not found or already deactivated.");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
            return Result.Failure("Invalid password.");

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Result.Failure(result.Errors.Select(e => e.Description));

        return Result.Success();
    }

    public async Task<Result> GeneratePasswordResetTokenAsync(string email, string baseUrl)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || user.IsDeleted)
            return Result.Success();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var resetLink = $"{baseUrl.TrimEnd('/')}/Account/ResetPassword?userId={user.Id}&token={encodedToken}";

        var emailBody = $@"
            <h2>Password Reset Request</h2>
            <p>You requested to reset your password. Click the link below to proceed:</p>
            <p><a href='{resetLink}'>Reset Password Link</a></p>";

        await _emailService.SendEmailAsync(user.Email!, "Password Reset Request - Sakanak", emailBody);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(string userId, string token, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return Result.Failure("User not found.");

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);
        
        if (!result.Succeeded)
            return Result.Failure(result.Errors.Select(e => e.Description));

        return Result.Success();
    }

    public AuthenticationProperties ConfigureExternalAuthenticationProperties(string provider, string redirectUrl, string? role = null)
    {
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        if (!string.IsNullOrEmpty(role))
        {
            properties.Items["role"] = role;
        }
        return properties;
    }

    public async Task<AuthResult> HandleExternalLoginCallbackAsync()
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return AuthResult.Fail("Error loading external login information.");

        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: false);

        if (result.Succeeded || result.RequiresTwoFactor)
        {
            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user == null || user.IsDeleted)
                return AuthResult.Fail("Unable to resolve account for this external login , User May be deactivated!");

            return await FinalizeSignInAsync(user, result);
        }

        if (result.IsLockedOut)
            return AuthResult.Fail("Account locked out.");

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
            return AuthResult.Fail("Email not found from Google provider.");

        var userWithEmail = await _userManager.FindByEmailAsync(email);
        if (userWithEmail != null)
        {
            if (userWithEmail.IsDeleted)
                return AuthResult.Fail("This account has been deactivated.");

            var addLoginResult = await _userManager.AddLoginAsync(userWithEmail, info);
            if (addLoginResult.Succeeded)
            {
                var linkedSignInResult = await _signInManager.ExternalLoginSignInAsync(
                    info.LoginProvider,
                    info.ProviderKey,
                    isPersistent: false,
                    bypassTwoFactor: false);

                return await FinalizeSignInAsync(userWithEmail, linkedSignInResult);
            }
            return AuthResult.Fail(addLoginResult.Errors.Select(e => e.Description));
        }

        // 🟢 NEW USER CASE: Return info for registration flow
        return AuthResult.ExternalRegistrationRequired(
            email, 
            info.Principal.FindFirstValue(ClaimTypes.Name) ?? email,
            info.LoginProvider,
            info.ProviderKey
        );
    }

    public async Task<AuthResult> CompleteExternalRegistrationAsync(ExternalRegisterRequest request)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return AuthResult.Fail("External registration session expired. Please sign in with Google again.");

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            return AuthResult.Fail("Google account email is required.");

        if (!string.Equals(request.Email?.Trim(), email, StringComparison.OrdinalIgnoreCase))
            return AuthResult.Fail("External account email mismatch.");

        var requestedUsername = request.Username?.Trim();
        if (string.IsNullOrWhiteSpace(requestedUsername))
            return AuthResult.Fail("Username is required.");

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            if (existingUser.IsDeleted)
                return AuthResult.Fail("This account has been deactivated.");

            var linkResult = await _userManager.AddLoginAsync(existingUser, info);
            if (!linkResult.Succeeded)
                return AuthResult.Fail(linkResult.Errors.Select(e => e.Description));

            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: false);

            return await FinalizeSignInAsync(existingUser, signInResult);
        }

        var existingUserByName = await _userManager.FindByNameAsync(requestedUsername);
        if (existingUserByName != null)
            return AuthResult.FailWithCode("UsernameTaken", "Username is already taken.");

        var newUser = new ApplicationUser
        {
            UserName = requestedUsername,
            Email = email,
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = true,
            Status = UserStatus.Active,
            RegistrationDate = DateTime.UtcNow,
            IsProfileComplete = true
        };

        await _unitOfWork.BeginTransactionAsync();

        var createResult = await _userManager.CreateAsync(newUser, request.Password);
        if (!createResult.Succeeded)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return AuthResult.Fail(createResult.Errors.Select(e => e.Description));
        }

        var addLoginResult = await _userManager.AddLoginAsync(newUser, info);
        if (!addLoginResult.Succeeded)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return AuthResult.Fail(addLoginResult.Errors.Select(e => e.Description));
        }

        var normalizedRole = NormalizeRole(request.Role);
        if (normalizedRole == null)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return AuthResult.Fail("Invalid role selected.");
        }

        await EnsureRoleExistsAsync(normalizedRole);
        var addRoleResult = await _userManager.AddToRoleAsync(newUser, normalizedRole);
        if (!addRoleResult.Succeeded)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return AuthResult.Fail(addRoleResult.Errors.Select(e => e.Description));
        }

        if (normalizedRole == "Student")
        {
            await _unitOfWork.Students.AddAsync(new Student
            {
                ApplicationUserId = newUser.Id,
                University = request.University ?? string.Empty,
                Faculty = request.Faculty ?? string.Empty,
                Age = request.Age
            });
        }
        else if (normalizedRole == "Landlord")
        {
            await _unitOfWork.Landlords.AddAsync(new Landlord
            {
                ApplicationUserId = newUser.Id,
                Age = request.Age,
                VerificationStatus = false
            });
        }

        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.CommitTransactionAsync();

        var signInResultForNewUser = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: false);

        return await FinalizeSignInAsync(newUser, signInResultForNewUser);
    }

    public async Task<bool> IsProfileCompleteAsync(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.PhoneNumber)) return false;

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault();

        if (role == "Student")
        {
            var student = await _unitOfWork.Students.GetByUserIdAsync(user.Id);
            if (student == null || string.IsNullOrWhiteSpace(student.University) || string.IsNullOrWhiteSpace(student.Faculty) || student.Age <= 0)
                return false;
        }
        else if (role == "Landlord")
        {
            var landlord = await _unitOfWork.Landlords.GetByUserIdAsync(user.Id);
            if (landlord == null || landlord.Age <= 0)
                return false;
        }

        return true;
    }

    public async Task<(string? Role, int Age, string? PhoneNumber)> GetOnboardingDataAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (null, 0, null);

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault();
        int age = 0;

        if (role == "Student")
        {
            var student = await _unitOfWork.Students.GetByUserIdAsync(user.Id);
            if (student != null) age = student.Age;
        }
        else if (role == "Landlord")
        {
            var landlord = await _unitOfWork.Landlords.GetByUserIdAsync(user.Id);
            if (landlord != null) age = landlord.Age;
        }

        return (role, age, user.PhoneNumber);
    }

    public async Task<Result> CompleteProfileAsync(string userId, string? password, string university, string faculty, string phoneNumber, int age)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return Result.Failure("User not found.");

        await _unitOfWork.BeginTransactionAsync();

        if (user.PasswordHash == null && !string.IsNullOrEmpty(password))
        {
            var passResult = await _userManager.AddPasswordAsync(user, password);
            if (!passResult.Succeeded)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result.Failure(passResult.Errors.Select(e => e.Description));
            }
        }

        user.PhoneNumber = phoneNumber.Trim();

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault();

        if (role == "Student")
        {
            var student = await _unitOfWork.Students.GetByUserIdAsync(user.Id);
            if (student != null)
            {
                student.University = university.Trim();
                student.Faculty = faculty.Trim();
                student.Age = age;
                await _unitOfWork.SaveChangesAsync();
            }
        }
        else if (role == "Landlord")
        {
            var landlord = await _unitOfWork.Landlords.GetByUserIdAsync(user.Id);
            if (landlord != null)
            {
                landlord.Age = age;
                await _unitOfWork.SaveChangesAsync();
            }
        }

        user.IsProfileComplete = true;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure(updateResult.Errors.Select(e => e.Description));
        }

        await _unitOfWork.CommitTransactionAsync();
        await _signInManager.RefreshSignInAsync(user);

        return Result.Success();
    }

    public async Task<Result> AddPasswordAsync(string userId, string password)
        => await SetPasswordForExternalUserAsync(userId, password);

    public async Task<Result> SetPasswordForExternalUserAsync(string userId, string password)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return Result.Failure("User not found.");

        if (user.PasswordHash != null)
            return Result.Failure("User already has a password.");

        var result = await _userManager.AddPasswordAsync(user, password);
        if (!result.Succeeded)
            return Result.Failure(result.Errors.Select(e => e.Description));

        return Result.Success();
    }

    private async Task<AuthResult> FinalizeSignInAsync(ApplicationUser user, SignInResult signInResult)
    {
        if (user.Status == UserStatus.Suspended)
            return AuthResult.Fail("Your account has been suspended. Please contact support.");

        if (signInResult.RequiresTwoFactor)
        {
            await SendTwoFactorCodeInternalAsync(user);
            return AuthResult.RequiresTwoFactor(user.Id.ToString());
        }

        if (signInResult.IsLockedOut)
            return AuthResult.Fail("Account locked out.");

        if (!signInResult.Succeeded)
            return AuthResult.Fail("Unable to sign in using external provider.");

        if (!await IsProfileCompleteAsync(user))
            return AuthResult.FailWithCode("IncompleteProfile", "Please complete your profile to continue.");

        var roles = await _userManager.GetRolesAsync(user);
        return AuthResult.Ok(roles.FirstOrDefault());
    }

    private static string? NormalizeRole(string raw) =>
        raw.Trim() switch
        {
            var r when string.Equals(r, "Student", StringComparison.OrdinalIgnoreCase) => "Student",
            var r when string.Equals(r, "Landlord", StringComparison.OrdinalIgnoreCase) => "Landlord",
            _ => null
        };

    private async Task EnsureRoleExistsAsync(string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
    }
}
