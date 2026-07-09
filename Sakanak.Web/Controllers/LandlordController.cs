using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sakanak.BLL.DTOs.Common;
using Sakanak.BLL.DTOs.Apartment;
using Sakanak.BLL.Interfaces;
using Sakanak.DAL.UnitOfWork;
using Sakanak.Domain.Enums;
using Sakanak.Web.Models.ViewModels.Landlord;
using Sakanak.Web.Models.ViewModels.Shared;

namespace Sakanak.Web.Controllers;

[Authorize(Roles = "Landlord")]
public class LandlordController : Controller
{
    private readonly IApartmentService _apartmentService;
    private readonly IRequestService _requestService;
    private readonly ILandlordVerificationService _verificationService;
    private readonly IBookingService _bookingService;
    private readonly IContractService _contractService;
    private readonly IPaymentService _paymentService;
    private readonly IApartmentAssignmentService _assignmentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly INotificationService _notificationService;
    private readonly IMessageService _messageService;
    private readonly Microsoft.AspNetCore.Identity.UserManager<Sakanak.Domain.Entities.ApplicationUser> _userManager;

    public LandlordController(
        IApartmentService apartmentService,
        IRequestService requestService,
        ILandlordVerificationService verificationService,
        IBookingService bookingService,
        IContractService contractService,
        IPaymentService paymentService,
        IApartmentAssignmentService assignmentService,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        INotificationService notificationService,
        IMessageService messageService,
        Microsoft.AspNetCore.Identity.UserManager<Sakanak.Domain.Entities.ApplicationUser> userManager)
    {
        _apartmentService = apartmentService;
        _requestService = requestService;
        _verificationService = verificationService;
        _bookingService = bookingService;
        _contractService = contractService;
        _paymentService = paymentService;
        _assignmentService = assignmentService;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _notificationService = notificationService;
        _messageService = messageService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Dashboard(
        int page = 1,
        int pageSize = 3,
        string? city = null,
        string? requestStatus = null,
        string sortBy = "address",
        bool desc = false)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var apartmentsResult = await _apartmentService.GetLandlordApartmentsAsync(userId.Value, page, pageSize, city, requestStatus, sortBy, desc);
        var requestsResult = await _requestService.GetLandlordRequestsAsync(userId.Value, 1, 5);
        var landlord = await GetCurrentLandlordAsync();
        var bookingsResult = landlord is null
            ? null
            : await _bookingService.GetLandlordBookingsAsync(landlord.LandlordId, statusFilter: BookingStatus.Pending);
        var contractsResult = landlord is null
            ? null
            : await _contractService.GetLandlordContractsAsync(landlord.LandlordId);
        var paymentsResult = landlord is null
            ? null
            : await _paymentService.GetLandlordPaymentsAsync(landlord.LandlordId);
        var occupancyResult = landlord is null
            ? null
            : await _assignmentService.GetLandlordApartmentsWithOccupancyAsync(landlord.LandlordId);

        if (!apartmentsResult.Succeeded || !requestsResult.Succeeded)
        {
            TempData["ErrorMessage"] = apartmentsResult.ErrorMessage ?? requestsResult.ErrorMessage ?? "Unable to load dashboard.";
            return View(new LandlordDashboardViewModel());
        }

        var apartmentViewModels = apartmentsResult.Data!.Items
            .Select(apartment => new ApartmentListItemViewModel
            {
                ApartmentId = apartment.ApartmentId,
                Address = apartment.Address,
                City = apartment.City,
                PricePerMonth = apartment.PricePerMonth,
                TotalSeats = apartment.TotalSeats,
                OccupiedSeats = apartment.OccupiedSeats,
                AvailableSeats = apartment.AvailableSeats,
                ActiveBookingCount = apartment.ActiveBookingCount,
                IsActive = apartment.IsActive,
                ApartmentStatusDisplay = apartment.ApartmentStatusDisplay,
                LatestRequestStatus = apartment.LatestRequestStatus,
                LatestRequestDate = apartment.LatestRequestDate,
                PrimaryPhotoUrl = apartment.PrimaryPhotoUrl
            })
            .ToList();

        var requestViewModels = requestsResult.Data!.Items
            .Select(MapRequestListItem)
            .ToList();

        var model = new LandlordDashboardViewModel
        {
            TotalApartments = apartmentsResult.Data.TotalCount,
            ActiveApartments = apartmentsResult.Data.Items.Count(item => item.IsActive),
            PendingRequests = requestViewModels.Count(item => item.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase)),
            RejectedRequests = requestViewModels.Count(item => item.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase)),
            PendingBookingRequests = bookingsResult?.Succeeded == true ? bookingsResult.Data!.Count : 0,
            Apartments = apartmentViewModels,
            RecentRequests = requestViewModels.Take(5).ToList(),
            RecentBookingRequests = bookingsResult?.Succeeded == true ? bookingsResult.Data!.Take(5).ToList() : new List<Sakanak.BLL.DTOs.Booking.BookingDto>(),
            RecentContracts = contractsResult?.Succeeded == true ? contractsResult.Data!.Take(5).ToList() : Array.Empty<Sakanak.BLL.DTOs.Contract.ContractDto>(),
            RecentPayments = paymentsResult?.Succeeded == true ? paymentsResult.Data!.Take(5).ToList() : Array.Empty<Sakanak.BLL.DTOs.Payment.PaymentDto>(),
            RecentOccupiedApartments = occupancyResult?.Succeeded == true ? occupancyResult.Data!.Where(item => item.OccupiedSeats > 0).Take(5).ToList() : new List<Sakanak.BLL.DTOs.Apartment.ApartmentOccupancyDto>(),
            PendingContracts = contractsResult?.Succeeded == true ? contractsResult.Data!.Count(item => item.Status == ContractStatus.PendingApproval.ToString()) : 0,
            ApprovedContracts = contractsResult?.Succeeded == true ? contractsResult.Data!.Count(item => item.Status == ContractStatus.Approved.ToString()) : 0,
            TotalPaidAmount = paymentsResult?.Succeeded == true ? paymentsResult.Data!.Where(item => item.Status == PaymentStatus.Paid.ToString()).Sum(item => item.Amount) : 0,
            PendingPaymentAmount = paymentsResult?.Succeeded == true ? paymentsResult.Data!.Where(item => item.Status == PaymentStatus.Pending.ToString()).Sum(item => item.Amount) : 0,
            OccupiedSeats = occupancyResult?.Succeeded == true ? occupancyResult.Data!.Sum(item => item.OccupiedSeats) : 0,
            TotalSeats = occupancyResult?.Succeeded == true ? occupancyResult.Data!.Sum(item => item.TotalSeats) : 0,
            OccupancyRate = occupancyResult?.Succeeded == true && occupancyResult.Data!.Sum(item => item.TotalSeats) > 0
                ? Math.Round((decimal)occupancyResult.Data!.Sum(item => item.OccupiedSeats) / occupancyResult.Data!.Sum(item => item.TotalSeats) * 100, 1)
                : 0,
            OccupiedApartmentsCount = occupancyResult?.Succeeded == true ? occupancyResult.Data!.Count(item => item.OccupiedSeats > 0) : 0,
            ApartmentsPagination = BuildPagination(
                apartmentsResult.Data,
                nameof(Dashboard),
                new Dictionary<string, string?>
                {
                    ["pageSize"] = pageSize.ToString(),
                    ["city"] = city,
                    ["requestStatus"] = requestStatus,
                    ["sortBy"] = sortBy,
                    ["desc"] = desc.ToString().ToLowerInvariant()
                }),
            CityFilter = city,
            RequestStatusFilter = requestStatus,
            SortBy = sortBy,
            Descending = desc,
            RecentNotifications = (await _notificationService.GetRecentNotificationsAsync(userId.Value, 3)).Data ?? new(),
            RecentMessages = (await _messageService.GetUserConversationsAsync(userId.Value)).Data?.Take(3).ToList() ?? new(),
            AdminUserId = (await _userManager.GetUsersInRoleAsync("Admin")).FirstOrDefault()?.Id
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateApartment()
    {
        if (!await IsCurrentLandlordVerifiedAsync())
        {
            return RedirectToAction(nameof(VerificationPending));
        }

        return View(new CreateApartmentViewModel
        {
            AvailableAmenities = GetConfiguredAmenities()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateApartment(CreateApartmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableAmenities = GetConfiguredAmenities();
            return View(model);
        }

        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!await IsCurrentLandlordVerifiedAsync())
        {
            return RedirectToAction(nameof(VerificationPending));
        }

        var dto = new CreateApartmentDto
        {
            Address = model.Address,
            City = model.City,
            PricePerMonth = model.PricePerMonth,
            TotalSeats = model.TotalSeats,
            Amenities = model.Amenities,
            VirtualTourUrl = model.VirtualTourUrl,
            Photos = model.Photos
        };

        var result = await _apartmentService.CreateApartmentAsync(dto, userId.Value);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            model.AvailableAmenities = GetConfiguredAmenities();
            return View(model);
        }

        TempData["SuccessMessage"] = "Apartment submitted for admin approval.";
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public async Task<IActionResult> EditApartment(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!await IsCurrentLandlordVerifiedAsync())
        {
            return RedirectToAction(nameof(VerificationPending));
        }

        var result = await _apartmentService.GetApartmentByIdAsync(id, userId.Value);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Dashboard));
        }

        var apartment = result.Data!;
        var model = new EditApartmentViewModel
        {
            ApartmentId = apartment.ApartmentId,
            Address = apartment.Address,
            City = apartment.City,
            PricePerMonth = apartment.PricePerMonth,
            TotalSeats = apartment.TotalSeats,
            Amenities = apartment.Amenities,
            VirtualTourUrl = apartment.VirtualTourUrl,
            ExistingPhotos = apartment.Photos.Select(photo => new ApartmentPhotoViewModel
            {
                MediaId = photo.MediaId,
                Url = photo.Url
            }).ToList(),
            AvailableAmenities = GetConfiguredAmenities(),
            LatestRequestStatus = apartment.LatestRequestStatus
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditApartment(int id, EditApartmentViewModel model)
    {
        if (id != model.ApartmentId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            model.AvailableAmenities = GetConfiguredAmenities();
            return View(model);
        }

        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!await IsCurrentLandlordVerifiedAsync())
        {
            return RedirectToAction(nameof(VerificationPending));
        }

        var dto = new UpdateApartmentDto
        {
            ApartmentId = model.ApartmentId,
            Address = model.Address,
            City = model.City,
            PricePerMonth = model.PricePerMonth,
            TotalSeats = model.TotalSeats,
            Amenities = model.Amenities,
            VirtualTourUrl = model.VirtualTourUrl,
            NewPhotos = model.NewPhotos,
            RemovedPhotoIds = model.RemovedPhotoIds
        };

        var result = await _apartmentService.UpdateApartmentAsync(dto, userId.Value);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            var reload = await _apartmentService.GetApartmentByIdAsync(id, userId.Value);
            if (reload.Succeeded)
            {
                model.ExistingPhotos = reload.Data!.Photos.Select(photo => new ApartmentPhotoViewModel
                {
                    MediaId = photo.MediaId,
                    Url = photo.Url
                }).ToList();
                model.LatestRequestStatus = reload.Data!.LatestRequestStatus;
            }

            model.AvailableAmenities = GetConfiguredAmenities();
            return View(model);
        }

        TempData["SuccessMessage"] = "Apartment updated successfully.";
        return RedirectToAction(nameof(ApartmentDetails), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> ApartmentDetails(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _apartmentService.GetApartmentByIdAsync(id, userId.Value);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Dashboard));
        }

        var apartment = result.Data!;
        return View(new ApartmentDetailsViewModel
        {
            ApartmentId = apartment.ApartmentId,
            Address = apartment.Address,
            City = apartment.City,
            PricePerMonth = apartment.PricePerMonth,
            TotalSeats = apartment.TotalSeats,
            AvailableSeats = apartment.AvailableSeats,
            OccupiedSeats = apartment.OccupiedSeats,
            Amenities = apartment.Amenities,
            VirtualTourUrl = apartment.VirtualTourUrl,
            IsActive = apartment.IsActive,
            LatestRequestStatus = apartment.LatestRequestStatus,
            LatestRequestMessage = apartment.LatestRequestMessage,
            LatestRequestDate = apartment.LatestRequestDate,
            LatestResolvedAt = apartment.LatestResolvedAt,
            Photos = apartment.Photos.Select(photo => new ApartmentPhotoViewModel
            {
                MediaId = photo.MediaId,
                Url = photo.Url
            }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!await IsCurrentLandlordVerifiedAsync())
        {
            return RedirectToAction(nameof(VerificationPending));
        }

        var result = await _apartmentService.ToggleApartmentActiveStatusAsync(id, userId.Value);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Apartment availability updated."
            : result.ErrorMessage ?? "Unable to update apartment status.";

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteApartment(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!await IsCurrentLandlordVerifiedAsync())
        {
            return RedirectToAction(nameof(VerificationPending));
        }

        var result = await _apartmentService.DeleteApartmentAsync(id, userId.Value);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Apartment deleted successfully."
            : result.ErrorMessage ?? "Unable to delete apartment.";

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public async Task<IActionResult> MyRequests(
        int page = 1,
        int pageSize = 3,
        string? status = null,
        string sortBy = "createdat",
        bool desc = true)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _requestService.GetLandlordRequestsAsync(userId.Value, page, pageSize, status, sortBy, desc);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new MyRequestsViewModel());
        }

        return View(new MyRequestsViewModel
        {
            Requests = result.Data!.Items.Select(MapRequestListItem).ToList(),
            Pagination = BuildPagination(
                result.Data,
                nameof(MyRequests),
                new Dictionary<string, string?>
                {
                    ["pageSize"] = pageSize.ToString(),
                    ["status"] = status,
                    ["sortBy"] = sortBy,
                    ["desc"] = desc.ToString().ToLowerInvariant()
                }),
            StatusFilter = status,
            SortBy = sortBy,
            Descending = desc
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRequest(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _requestService.CancelPendingRequestAsync(id, userId.Value);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Request cancelled successfully."
            : result.ErrorMessage ?? "Unable to cancel request.";

        return RedirectToAction(nameof(MyRequests));
    }

    [HttpGet]
    public async Task<IActionResult> BookingRequests(int? apartmentId = null, BookingStatus? status = null)
    {
        var landlord = await GetCurrentLandlordAsync();
        if (landlord is null)
        {
            return Challenge();
        }

        var result = await _bookingService.GetLandlordBookingsAsync(landlord.LandlordId, apartmentId, status);
        var allBookingsResult = await _bookingService.GetLandlordBookingsAsync(landlord.LandlordId);
        var apartments = await _unitOfWork.Apartments.GetByLandlordIdAsync(landlord.LandlordId);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new BookingRequestsViewModel());
        }

        var allBookings = allBookingsResult.Succeeded ? allBookingsResult.Data! : new List<Sakanak.BLL.DTOs.Booking.BookingDto>();
        return View(new BookingRequestsViewModel
        {
            Bookings = result.Data!,
            ApartmentId = apartmentId,
            StatusFilter = status,
            Apartments = apartments.Select(item => new ApartmentOptionViewModel
            {
                ApartmentId = item.ApartmentId,
                Address = item.Address
            }).ToList(),
            PendingCount = allBookings.Count(item => item.Status == BookingStatus.Pending.ToString()),
            AcceptedCount = allBookings.Count(item => item.Status == BookingStatus.Accepted.ToString()),
            RejectedCount = allBookings.Count(item => item.Status == BookingStatus.Rejected.ToString())
        });
    }

    [HttpGet]
    public async Task<IActionResult> ReviewBooking(int id)
    {
        var landlord = await GetCurrentLandlordAsync();
        if (landlord is null)
        {
            return Challenge();
        }

        var result = await _bookingService.GetBookingDetailsForLandlordAsync(id, landlord.LandlordId);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(BookingRequests));
        }

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptBooking(int id)
    {
        var landlord = await GetCurrentLandlordAsync();
        if (landlord is null)
        {
            return Challenge();
        }

        var result = await _bookingService.AcceptBookingAsync(id, landlord.LandlordId);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Booking accepted successfully."
            : result.ErrorMessage ?? "Unable to accept booking.";

        return RedirectToAction(nameof(ReviewBooking), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectBooking(int id, string reason)
    {
        var landlord = await GetCurrentLandlordAsync();
        if (landlord is null)
        {
            return Challenge();
        }

        var result = await _bookingService.RejectBookingAsync(id, landlord.LandlordId, reason);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Booking rejected."
            : result.ErrorMessage ?? "Unable to reject booking.";

        return RedirectToAction(nameof(ReviewBooking), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Contracts(int? apartmentId = null)
    {
        var landlord = await GetCurrentLandlordAsync();
        if (landlord is null)
        {
            return Challenge();
        }

        var result = await _contractService.GetLandlordContractsAsync(landlord.LandlordId, apartmentId);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new List<Sakanak.BLL.DTOs.Contract.ContractDto>());
        }

        return View(result.Data!);
    }

    [HttpGet]
    public async Task<IActionResult> ContractDetails(int id)
    {
        var landlord = await GetCurrentLandlordAsync();
        if (landlord is null)
        {
            return Challenge();
        }

        var result = await _contractService.GetContractDetailsForLandlordAsync(id, landlord.LandlordId);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Contracts));
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadContractDocument(int id)
    {
        var landlord = await GetCurrentLandlordAsync();
        if (landlord is null)
        {
            return Challenge();
        }

        var result = await _contractService.GetContractDetailsForLandlordAsync(id, landlord.LandlordId);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Data!.DocumentUrl))
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Contract document was not found.";
            return RedirectToAction(nameof(Contracts));
        }

        return Redirect(Url.Content(result.Data.DocumentUrl));
    }

    [HttpGet]
    public async Task<IActionResult> Payments()
    {
        var landlord = await GetCurrentLandlordAsync();
        if (landlord is null)
        {
            return Challenge();
        }

        var result = await _paymentService.GetLandlordPaymentsAsync(landlord.LandlordId);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new List<Sakanak.BLL.DTOs.Payment.PaymentDto>());
        }

        return View(result.Data!);
    }

    [HttpGet]
    public async Task<IActionResult> PaymentDetails(int id)
    {
        var landlord = await GetCurrentLandlordAsync();
        if (landlord is null)
        {
            return Challenge();
        }

        var result = await _paymentService.GetPaymentDetailsAsync(id, landlord.LandlordId);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Payments));
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> MyApartments()
    {
        var landlord = await GetCurrentLandlordAsync();
        if (landlord is null)
        {
            return Challenge();
        }

        var result = await _assignmentService.GetLandlordApartmentsWithOccupancyAsync(landlord.LandlordId);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new List<Sakanak.BLL.DTOs.Apartment.ApartmentOccupancyDto>());
        }

        return View(result.Data!);
    }

    [HttpGet]
    public async Task<IActionResult> OccupiedApartments()
    {
        var landlord = await GetCurrentLandlordAsync();
        if (landlord is null)
        {
            return Challenge();
        }

        var result = await _assignmentService.GetLandlordApartmentsWithOccupancyAsync(landlord.LandlordId);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new List<Sakanak.BLL.DTOs.Apartment.ApartmentOccupancyDto>());
        }

        var occupiedOnly = result.Data!.Where(item => item.OccupiedSeats > 0).ToList();
        return View(occupiedOnly);
    }

    [HttpGet]
    public async Task<IActionResult> ApartmentTenants(int apartmentId)
    {
        var landlord = await GetCurrentLandlordAsync();
        if (landlord is null)
        {
            return Challenge();
        }

        var result = await _assignmentService.GetApartmentTenantsAsync(apartmentId, landlord.LandlordId);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(MyApartments));
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> VerificationPending()
    {
        var landlord = await GetCurrentLandlordAsync();
        if (landlord is null)
        {
            return Challenge();
        }

        var result = await _verificationService.GetVerificationStatusAsync(landlord.LandlordId);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Dashboard));
        }

        return View(result.Data);
    }

    [HttpGet]
    public IActionResult SubmitVerification()
    {
        return View(new SubmitVerificationViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitVerification(SubmitVerificationViewModel model)
    {
        var landlord = await GetCurrentLandlordAsync();
        if (landlord is null)
        {
            return Challenge();
        }

        var result = await _verificationService.SubmitVerificationDocumentsAsync(landlord.LandlordId, model.Documents);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Verification documents submitted for admin review."
            : result.ErrorMessage ?? "Unable to submit verification documents.";

        return result.Succeeded ? RedirectToAction(nameof(VerificationPending)) : View(model);
    }

    private Guid? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }

    private async Task<Sakanak.Domain.Entities.Landlord?> GetCurrentLandlordAsync()
    {
        var userId = GetCurrentUserId();
        return userId is null ? null : await _unitOfWork.Landlords.GetByUserIdAsync(userId.Value);
    }

    private async Task<bool> IsCurrentLandlordVerifiedAsync()
        => (await GetCurrentLandlordAsync())?.VerificationStatus == true;

    private string[] GetConfiguredAmenities()
        => _configuration.GetSection("Amenities").Get<string[]>() ??
           ["WiFi", "AC", "Heating", "Parking", "Furnished", "Kitchen", "Laundry", "Balcony", "Security", "Elevator"];

    private static RequestListItemViewModel MapRequestListItem(Sakanak.BLL.DTOs.Request.RequestListItemDto dto)
    {
        return new RequestListItemViewModel
        {
            RequestId = dto.RequestId,
            ApartmentId = dto.ApartmentId,
            ApartmentAddress = dto.ApartmentAddress,
            City = dto.City,
            LandlordName = dto.LandlordName,
            LandlordEmail = dto.LandlordEmail,
            Status = dto.Status,
            Message = dto.Message,
            CreatedAt = dto.CreatedAt,
            ResolvedAt = dto.ResolvedAt,
            ReviewedByAdminName = dto.ReviewedByAdminName,
            ThumbnailUrl = dto.ThumbnailUrl
        };
    }

    private PaginationViewModel BuildPagination<T>(
        PagedResult<T> pagedResult,
        string action,
        Dictionary<string, string?> routeValues)
    {
        return new PaginationViewModel
        {
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalCount = pagedResult.TotalCount,
            TotalPages = pagedResult.TotalPages,
            HasPrevious = pagedResult.HasPrevious,
            HasNext = pagedResult.HasNext,
            Action = action,
            Controller = "Landlord",
            RouteValues = routeValues
        };
    }
}
