using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sakanak.BLL.Interfaces;
using Sakanak.Web.Models.ViewModels.Account;
using System.Threading.Tasks;

namespace Sakanak.Web.Controllers;

[AllowAnonymous]
public class TwoFactorController : Controller
{
    private readonly IAuthService _authService;

    public TwoFactorController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult VerifyCode(string userId, string? returnUrl = null)
    {
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var model = new VerifyCodeViewModel
        {
            UserId = userId,
            ReturnUrl = returnUrl
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var isSuccess = await _authService.VerifyTwoFactorCodeAsync(model.UserId, model.Code);
        if (isSuccess)
        {
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        ModelState.AddModelError(string.Empty, "Invalid or expired 2FA code.");
        return View(model);
    }
}
