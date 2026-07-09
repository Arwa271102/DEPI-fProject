using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Sakanak.BLL.DTOs.Auth;
using Sakanak.BLL.DTOs.Profile;
using Sakanak.BLL.Interfaces;
using Sakanak.Web.Models.ViewModels.Account;
using System.Security.Claims;
using Sakanak.Domain.Entities;

namespace Sakanak.Web.Controllers;

/// <summary>
/// Handles authentication UI flows (Login, Register, Logout, AccessDenied).
/// Controllers must only call IAuthService — no Identity or repository usage here.
/// </summary>
public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IProfileService _profileService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        IAuthService authService, 
        IProfileService profileService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _authService = authService;
        _profileService = profileService;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // ─── Login ───────────────────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        await _authService.GeneratePasswordResetTokenAsync(model.Email, baseUrl);

        TempData["SuccessMessage"] = "If an account with that email exists, a reset link has been sent.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            return RedirectToAction(nameof(Login));

        return View(new ResetPasswordViewModel { UserId = userId, Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.ResetPasswordAsync(model.UserId, model.Token, model.NewPassword);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Password reset successfully. You can now login with your new password.";
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error);

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.LoginAsync(new LoginRequest
        {
            Email = model.Email,
            Password = model.Password,
            RememberMe = model.RememberMe
        });

        if (!result.Succeeded)
        {
            if (result.Code == "RequiresTwoFactor")
            {
                return RedirectToAction("VerifyCode", "TwoFactor", new { userId = result.UserId, returnUrl });
            }

            if (result.Code == "EmailNotConfirmed")
            {
                ViewBag.EmailNotConfirmed = true;
                ViewBag.UnconfirmedEmail = model.Email;
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
    }

    // ─── Register ────────────────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RegisterStudent(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewBag.Role = "Student";
        return View("Register", new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterStudent(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewBag.Role = "Student";

        if (string.IsNullOrWhiteSpace(model.University))
            ModelState.AddModelError(nameof(model.University), "University is required for students.");
        if (string.IsNullOrWhiteSpace(model.Faculty))
            ModelState.AddModelError(nameof(model.Faculty), "Faculty is required for students.");

        if (!ModelState.IsValid)
            return View("Register", model);

        return await ProcessRegistration(model, "Student", returnUrl);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? role = null, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _authService.ConfigureExternalAuthenticationProperties(provider, redirectUrl!, role);
        return new ChallengeResult(provider, properties);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError != null)
        {
            TempData["ErrorMessage"] = $"Error from external provider: {remoteError}";
            return RedirectToAction(nameof(Login));
        }

        var result = await _authService.HandleExternalLoginCallbackAsync();

        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        if (result.Code == "RequiresExternalRegistration")
        {
            TempData["ExternalEmail"] = result.ExternalEmail;
            TempData["ExternalName"] = result.ExternalName;
            
            TempData["InfoMessage"] = "Account not found. Please select your role to continue.";
            return RedirectToAction(nameof(SelectRole));
        }

        if (result.Code == "IncompleteProfile")
        {
            TempData["InfoMessage"] = "You've successfully signed in with Google. Now, please complete your profile.";
            return RedirectToAction(nameof(CompleteProfile));
        }

        if (result.Code == "RequiresTwoFactor")
        {
            return RedirectToAction("VerifyCode", "TwoFactor", new { userId = result.UserId, returnUrl });
        }

        foreach (var error in result.Errors)
            TempData["ErrorMessage"] = error;

        return RedirectToAction(nameof(Login));
    }

    // ─── External Registration Flow ─────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public IActionResult SelectRole()
    {
        if (TempData.Peek("ExternalEmail") == null)
            return RedirectToAction(nameof(Login));

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult SelectRole(string role)
    {
        if (string.IsNullOrEmpty(role))
        {
            ModelState.AddModelError("", "Please select a role.");
            return View();
        }

        TempData["SelectedRole"] = role;
        return RedirectToAction(nameof(ExternalRegistration));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ExternalRegistration()
    {
        if (TempData.Peek("ExternalEmail") == null || TempData.Peek("SelectedRole") == null)
            return RedirectToAction(nameof(SelectRole));

        var model = new ExternalRegisterViewModel
        {
            Email = TempData.Peek("ExternalEmail")?.ToString() ?? "",
            Name = TempData.Peek("ExternalName")?.ToString() ?? "",
            Role = TempData.Peek("SelectedRole")?.ToString() ?? "",
            Username = (TempData.Peek("ExternalEmail")?.ToString() ?? "").Split('@')[0]
        };

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalRegistration(ExternalRegisterViewModel model)
    {
        if (model.Role == "Landlord")
        {
            ModelState.Remove(nameof(model.University));
            ModelState.Remove(nameof(model.Faculty));
        }

        if (!ModelState.IsValid)
            return View(model);

        var request = new ExternalRegisterRequest
        {
            Email = model.Email,
            Name = model.Name,
            Username = model.Username,
            Role = model.Role,
            Password = model.Password,
            PhoneNumber = model.PhoneNumber,
            Age = model.Age,
            University = model.University,
            Faculty = model.Faculty
        };

        var result = await _authService.CompleteExternalRegistrationAsync(request);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Registration successful! Welcome to Sakanak.";
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error);

        return View(model);
    }

    // ─── Profile Completion Onboarding ─────────────────────────────────────

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> CompleteProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);

        if (user == null) return RedirectToAction(nameof(Login));

        // If already complete, redirect to dashboard
        if (await _authService.IsProfileCompleteAsync(user))
            return RedirectToAction("Index", "Dashboard");

        var (role, age, phoneNumber) = await _authService.GetOnboardingDataAsync(userId!);

        var model = new CompleteProfileViewModel
        {
            NeedsPassword = user.PasswordHash == null,
            Role = role,
            PhoneNumber = phoneNumber ?? string.Empty,
            Age = age
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteProfile(CompleteProfileViewModel model)
    {
        // For Landlord, University and Faculty are not required
        if (model.Role == "Landlord")
        {
            ModelState.Remove(nameof(model.University));
            ModelState.Remove(nameof(model.Faculty));
        }

        if (!ModelState.IsValid)
            return View(model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _authService.CompleteProfileAsync(userId!, model.Password, model.University ?? "", model.Faculty ?? "", model.PhoneNumber, model.Age);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Profile completed successfully! Welcome to Sakanak.";
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error);

        return View(model);
    }

    // ─── Register Landlord ───────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RegisterLandlord(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewBag.Role = "Landlord";
        return View("Register", new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterLandlord(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewBag.Role = "Landlord";

        if (!ModelState.IsValid)
            return View("Register", model);

        return await ProcessRegistration(model, "Landlord", returnUrl);
    }

    private async Task<IActionResult> ProcessRegistration(RegisterViewModel model, string role, string? returnUrl)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var result = await _authService.RegisterAsync(new RegisterRequest
        {
            Name = model.Name,
            Username = model.Username,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            Age = model.Age,
            University = model.University,
            Faculty = model.Faculty,
            Password = model.Password,
            BaseUrl = baseUrl
        }, role);

        if (!result.Succeeded)
        {
            if (result.Code == "UserExistsNotConfirmed")
            {
                ViewBag.UserExistsNotConfirmed = true;
                ViewBag.UnconfirmedEmail = model.Email;
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            return View("Register", model);
        }

        return RedirectToAction(nameof(CheckYourEmail));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult CheckYourEmail()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            return View("Error");

        var result = await _authService.ConfirmEmailAsync(userId, token);

        if (result.Succeeded)
            return View("EmailConfirmed");

        return View("Error");
    }

    // ─── Resend Confirmation ─────────────────────────────────────────────────

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendConfirmation(string email)
    {
        if (!string.IsNullOrEmpty(email))
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            await _authService.ResendConfirmationEmailAsync(email, baseUrl);
        }

        TempData["ResendMessage"] = "If an account exists, a confirmation email has been sent.";
        return RedirectToAction("Login");
    }

    // ─── Logout ──────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Settings()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

        var model = await LoadSettingsViewModelAsync(userId);
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel profile)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

        if (!ModelState.IsValid)
        {
            var vm = await LoadSettingsViewModelAsync(userId);
            vm.Profile = profile;
            return View(nameof(Settings), vm);
        }

        var result = await _profileService.UpdateProfileAsync(new UpdateProfileDto
        {
            Name = profile.Name,
            Username = profile.Username,
            PhoneNumber = profile.PhoneNumber,
            Age = profile.Age
        }, userId);

        if (result.IsSuccess)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _signInManager.RefreshSignInAsync(user);
            }
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Settings));
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Update failed");
        var errorVm = await LoadSettingsViewModelAsync(userId);
        errorVm.Profile = profile;
        return View(nameof(Settings), errorVm);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestEmailChange(ChangeEmailViewModel email)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

        if (!ModelState.IsValid)
        {
            var vm = await LoadSettingsViewModelAsync(userId);
            vm.Email = email;
            return View(nameof(Settings), vm);
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _profileService.RequestEmailChangeAsync(userId, email.NewEmail, baseUrl);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Confirmation email sent. Please check your new email address.";
            return RedirectToAction(nameof(Settings));
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Request failed");
        var errorVm = await LoadSettingsViewModelAsync(userId);
        errorVm.Email = email;
        return View(nameof(Settings), errorVm);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmailChange(string userId, string newEmail, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newEmail) || string.IsNullOrEmpty(token))
            return View("Error");

        var result = await _profileService.ConfirmEmailChangeAsync(userId, newEmail, token);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Email updated successfully";
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _signInManager.RefreshSignInAsync(user);
            }
            
            return RedirectToAction(nameof(Settings), "Account");
        }

        return View("Error");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel password)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

        if (!ModelState.IsValid)
        {
            var vm = await LoadSettingsViewModelAsync(userId);
            vm.Password = password;
            return View(nameof(Settings), vm);
        }

        var result = await _profileService.ChangePasswordAsync(userId, password.OldPassword, password.NewPassword);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction(nameof(Settings));
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Change failed");
        var errorVm = await LoadSettingsViewModelAsync(userId);
        errorVm.Password = password;
        return View(nameof(Settings), errorVm);
    }

    private async Task<SettingsViewModel> LoadSettingsViewModelAsync(string userId)
    {
        var profile = await _profileService.GetProfileAsync(userId);
        var is2faEnabled = await _authService.IsTwoFactorEnabledAsync(userId);

        return new SettingsViewModel
        {
            IsTwoFactorEnabled = is2faEnabled,
            Profile = new UpdateProfileViewModel
            {
                Name = profile.Name,
                Username = profile.Username,
                PhoneNumber = profile.PhoneNumber,
                Age = profile.Age,
                Email = profile.Email
            }
        };
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable2FA()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

        var result = await _authService.EnableTwoFactorAsync(userId);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Two-Factor Authentication has been successfully enabled.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(", ", result.Errors);
        }

        return RedirectToAction(nameof(Settings));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Disable2FA()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

        // We must verify the user actually has 2FA enabled before allowing them to disable it
        var is2faEnabled = await _authService.IsTwoFactorEnabledAsync(userId);
        if (!is2faEnabled)
        {
            return RedirectToAction(nameof(Settings));
        }

        // Send OTP code to verify disable intent
        var sendResult = await _authService.SendTwoFactorCodeAsync(userId);
        if (!sendResult.Succeeded)
        {
            TempData["ErrorMessage"] = "Failed to send 2FA disable code. Please try again later.";
            return RedirectToAction(nameof(Settings));
        }

        return View(new Disable2FAViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable2FA(Disable2FAViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

        var result = await _authService.DisableTwoFactorAsync(userId, model.Code);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Two-Factor Authentication has been successfully disabled.";
            return RedirectToAction(nameof(Settings));
        }

        ModelState.AddModelError(string.Empty, result.Errors.FirstOrDefault() ?? "Invalid code.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateAccount(DeactivateAccountViewModel deactivation)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

        if (!ModelState.IsValid)
        {
            var vm = await LoadSettingsViewModelAsync(userId);
            vm.Deactivation = deactivation;
            return View(nameof(Settings), vm);
        }

        var result = await _authService.DeactivateAccountAsync(userId, deactivation.Password);

        if (result.Succeeded)
        {
            await _signInManager.SignOutAsync();
            TempData["SuccessMessage"] = "Your account has been deactivated.";
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Deactivation failed");
        var errorVm = await LoadSettingsViewModelAsync(userId);
        errorVm.Deactivation = deactivation;
        return View(nameof(Settings), errorVm);
    }

    // ─── Access Denied ───────────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
