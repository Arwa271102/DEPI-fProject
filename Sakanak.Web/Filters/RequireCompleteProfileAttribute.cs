using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Sakanak.BLL.Interfaces;
using System.Security.Claims;

namespace Sakanak.Web.Filters;

public class RequireCompleteProfileAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            // Skip check for the CompleteProfile action itself and Logout
            var actionName = context.RouteData.Values["action"]?.ToString();
            var controllerName = context.RouteData.Values["controller"]?.ToString();

            if (controllerName == "Account" && (actionName == "CompleteProfile" || actionName == "Logout"))
            {
                await next();
                return;
            }

            if (controllerName == "Student" && (actionName == "Profile" || actionName == "Questionnaire" || actionName == "UploadProfilePhoto" || actionName == "DeleteProfilePhoto"))
            {
                await next();
                return;
            }

            // Check if profile is complete using the claim
            // We can add a claim for IsProfileComplete to avoid DB checks on every request,
            // but for now, we'll use the service.
            var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
            var userManager = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Sakanak.Domain.Entities.ApplicationUser>>();
            
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var appUser = await userManager.FindByIdAsync(userId!);

            if (appUser != null && !appUser.IsProfileComplete)
            {
                // Verify with logic just in case the flag is stale
                if (!await authService.IsProfileCompleteAsync(appUser))
                {
                    context.Result = new RedirectToActionResult("CompleteProfile", "Account", null);
                    return;
                }
                else
                {
                    // Flag was stale, update it
                    appUser.IsProfileComplete = true;
                    await userManager.UpdateAsync(appUser);
                }
            }
        }

        await next();
    }
}
