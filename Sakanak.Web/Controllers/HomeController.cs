using Microsoft.AspNetCore.Mvc;
using Sakanak.BLL.DTOs.Search;
using Sakanak.BLL.Interfaces;
using Sakanak.Web.Models;
using System.Diagnostics;

namespace Sakanak.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IApartmentSearchService _apartmentSearchService;

        public HomeController(ILogger<HomeController> logger, IApartmentSearchService apartmentSearchService)
        {
            _logger = logger;
            _apartmentSearchService = apartmentSearchService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                if (User.IsInRole("Landlord"))
                    return RedirectToAction("Dashboard", "Landlord");
                if (User.IsInRole("Student"))
                    return RedirectToAction("Dashboard", "Student");
            }

            var searchResult = await _apartmentSearchService.SearchApartmentsAsync(new ApartmentSearchDto
            {
                PageSize = 3,
                SortBy = "CreatedDate",
                Descending = true
            });

            var viewModel = new Sakanak.Web.Models.ViewModels.HomeViewModel();
            if (searchResult.IsSuccess && searchResult.Data != null)
            {
                viewModel.FeaturedApartments = searchResult.Data.Items.ToList();
            }

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
