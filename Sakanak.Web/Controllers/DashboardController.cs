using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sakanak.Web.Controllers;

/// <summary>
/// Thin dispatcher — redirects authenticated users to their role-specific dashboard.
/// Contains no business logic; all role-specific views live in their own controllers.
/// </summary>
[Authorize]
public class DashboardController : Controller
{
    /// <summary>
    /// Inspects the current user's role and redirects to the appropriate
    /// role-specific controller (Admin / Landlord / Student).
    /// </summary>
    public IActionResult Index()
    {
        if (User.IsInRole("Admin"))
            return RedirectToAction(nameof(AdminController.Dashboard), "Admin");

        if (User.IsInRole("Landlord"))
            return RedirectToAction(nameof(LandlordController.Dashboard), "Landlord");

        return RedirectToAction(nameof(StudentController.Dashboard), "Student");
    }
}
