using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sakanak.BLL.DTOs.Common;
using Sakanak.BLL.DTOs.Request;
using Sakanak.BLL.Interfaces;
using Sakanak.Domain.Enums;
using Sakanak.Web.Models.ViewModels.Admin;
using Sakanak.Web.Models.ViewModels.Landlord;
using Sakanak.Web.Models.ViewModels.Shared;

namespace Sakanak.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IRequestService _requestService;
    private readonly IAdminService _adminService;
    private readonly IContractService _contractService;
    private readonly IPaymentService _paymentService;
    private readonly IApartmentAssignmentService _assignmentService;
    private readonly INotificationService _notificationService;
    private readonly IMessageService _messageService;

    public AdminController(
        IRequestService requestService, 
        IAdminService adminService, 
        IContractService contractService, 
        IPaymentService paymentService, 
        IApartmentAssignmentService assignmentService,
        INotificationService notificationService,
        IMessageService messageService)
    {
        _requestService = requestService;
        _adminService = adminService;
        _contractService = contractService;
        _paymentService = paymentService;
        _assignmentService = assignmentService;
        _notificationService = notificationService;
        _messageService = messageService;
    }

    public async Task<IActionResult> Dashboard()
    {
        var pendingResult = await _requestService.GetPendingRequestsAsync(1, 3);
        var historyResult = await _requestService.GetRequestHistoryAsync(1, 100);
        var landlordsResult = await _adminService.GetAllLandlordsAsync(1, 100);
        var apartmentsResult = await _adminService.GetAllApartmentsAsync(1, 100);
        var pendingLandlordsResult = await _adminService.GetPendingVerificationsAsync(1, 5);
        var pendingContractsResult = await _contractService.GetPendingContractsAsync(1, 5);
        var contractHistoryResult = await _contractService.GetContractHistoryAsync(1, 5);
        var paymentsResult = await _paymentService.GetAllPaymentsAsync(1, 100);
        var occupancyResult = await _assignmentService.GetAllApartmentsWithOccupancyAsync();

        if (!pendingResult.Succeeded || !historyResult.Succeeded || !landlordsResult.Succeeded || !apartmentsResult.Succeeded || !pendingLandlordsResult.Succeeded || !pendingContractsResult.Succeeded || !contractHistoryResult.Succeeded || !paymentsResult.Succeeded || !occupancyResult.Succeeded)
        {
            TempData["ErrorMessage"] = pendingResult.ErrorMessage
                ?? historyResult.ErrorMessage
                ?? landlordsResult.ErrorMessage
                ?? apartmentsResult.ErrorMessage
                ?? pendingLandlordsResult.ErrorMessage
                ?? pendingContractsResult.ErrorMessage
                ?? contractHistoryResult.ErrorMessage
                ?? paymentsResult.ErrorMessage
                ?? occupancyResult.ErrorMessage
                ?? "Unable to load admin dashboard.";
            return View(new AdminDashboardViewModel());
        }

        var history = historyResult.Data!;
        var landlords = landlordsResult.Data!;
        var apartments = apartmentsResult.Data!;
        var pendingLandlords = pendingLandlordsResult.Data!;
        var payments = paymentsResult.Data!.Items;
        var occupancy = occupancyResult.Data!;
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var adminUserId = GetCurrentUserId();
        var notificationsResult = adminUserId.HasValue ? await _notificationService.GetRecentNotificationsAsync(adminUserId.Value, 3) : null;
        var messagesResult = adminUserId.HasValue ? await _messageService.GetUserConversationsAsync(adminUserId.Value) : null;

        var model = new AdminDashboardViewModel
        {
            PendingRequestsCount = pendingResult.Data!.TotalCount,
            TotalReviewedRequests = history.TotalCount,
            ApprovedRequestsCount = history.Items.Count(item => item.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase)),
            RejectedRequestsCount = history.Items.Count(item => item.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase)),
            TotalLandlords = landlords.TotalCount,
            SuspendedLandlords = landlords.Items.Count(item => item.Status.Equals(UserStatus.Suspended.ToString(), StringComparison.OrdinalIgnoreCase)),
            TotalApartments = apartments.TotalCount,
            PendingLandlordVerificationsCount = pendingLandlords.TotalCount,
            PendingContractsCount = pendingContractsResult.Data!.TotalCount,
            TotalPaidPaymentsAmount = payments.Where(item => item.Status == PaymentStatus.Paid.ToString()).Sum(item => item.Amount),
            PendingPaymentsAmount = payments.Where(item => item.Status == PaymentStatus.Pending.ToString()).Sum(item => item.Amount),
            PaymentsThisMonthAmount = payments.Where(item => item.Status == PaymentStatus.Paid.ToString() && item.PaidAt >= monthStart).Sum(item => item.Amount),
            PendingPaymentsCount = payments.Count(item => item.Status == PaymentStatus.Pending.ToString()),
            PlatformTotalSeats = occupancy.Sum(item => item.TotalSeats),
            PlatformOccupiedSeats = occupancy.Sum(item => item.OccupiedSeats),
            PlatformOccupancyRate = occupancy.Sum(item => item.TotalSeats) > 0
                ? Math.Round((decimal)occupancy.Sum(item => item.OccupiedSeats) / occupancy.Sum(item => item.TotalSeats) * 100, 1)
                : 0,
            ActiveContractsCount = occupancy.Sum(item => item.OccupiedSeats),
            OldestPendingVerificationRequestedAt = pendingLandlords.Items.FirstOrDefault()?.VerificationRequestedAt,
            RecentPendingRequests = pendingResult.Data!.Items.Take(3).Select(MapPendingRequest).ToList(),
            RecentPendingLandlords = pendingLandlords.Items,
            RecentPendingContracts = pendingContractsResult.Data.Items,
            RecentReviewedContracts = contractHistoryResult.Data!.Items,
            RecentPayments = payments.Take(5).ToList(),
            RecentNotifications = notificationsResult?.Succeeded == true ? notificationsResult.Data! : new(),
            RecentMessages = messagesResult?.Succeeded == true ? messagesResult.Data!.Take(3).ToList() : new()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> PendingContracts(int page = 1, int pageSize = 10)
    {
        var result = await _contractService.GetPendingContractsAsync(page, pageSize);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new Sakanak.BLL.DTOs.Common.PagedResult<Sakanak.BLL.DTOs.Contract.ContractDto>());
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> ContractHistory(int page = 1, int pageSize = 10)
    {
        var result = await _contractService.GetContractHistoryAsync(page, pageSize);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View("PendingContracts", new Sakanak.BLL.DTOs.Common.PagedResult<Sakanak.BLL.DTOs.Contract.ContractDto>());
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> ReviewContract(int id)
    {
        var result = await _contractService.GetContractDetailsForAdminAsync(id);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(PendingContracts));
        }

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveContract(int id)
    {
        var adminUserId = GetCurrentUserId();
        if (adminUserId is null)
        {
            return Challenge();
        }

        var result = await _contractService.ApproveContractAsync(id, adminUserId.Value);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Contract approved." : result.ErrorMessage ?? "Unable to approve contract.";
        return RedirectToAction(nameof(ReviewContract), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectContract(int id, string reason)
    {
        var adminUserId = GetCurrentUserId();
        if (adminUserId is null)
        {
            return Challenge();
        }

        var result = await _contractService.RejectContractAsync(id, adminUserId.Value, reason);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Contract rejected." : result.ErrorMessage ?? "Unable to reject contract.";
        return RedirectToAction(nameof(ReviewContract), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelContract(int id, string reason)
    {
        var adminUserId = GetCurrentUserId();
        if (adminUserId is null)
        {
            return Challenge();
        }

        var result = await _contractService.AdminCancelContractAsync(id, adminUserId.Value, reason);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Contract cancelled." : result.ErrorMessage ?? "Unable to cancel contract.";
        return RedirectToAction(nameof(ReviewContract), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadContractDocument(int id)
    {
        var result = await _contractService.GetContractDetailsForAdminAsync(id);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Data!.DocumentUrl))
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Contract document was not found.";
            return RedirectToAction(nameof(PendingContracts));
        }

        return Redirect(Url.Content(result.Data.DocumentUrl));
    }

    [HttpGet]
    public async Task<IActionResult> Payments(int page = 1, int pageSize = 10, PaymentStatus? status = null)
    {
        var result = await _paymentService.GetAllPaymentsAsync(page, pageSize, status);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new PagedResult<Sakanak.BLL.DTOs.Payment.PaymentDto>());
        }

        ViewBag.StatusFilter = status;
        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> PaymentDetails(int id)
    {
        var result = await _paymentService.GetPaymentDetailsForAdminAsync(id);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Payments));
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> ApartmentsOccupancy()
    {
        var result = await _assignmentService.GetAllApartmentsWithOccupancyAsync();
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new List<Sakanak.BLL.DTOs.Apartment.ApartmentOccupancyDto>());
        }

        return View(result.Data!);
    }

    [HttpGet]
    public async Task<IActionResult> ApartmentOccupancyDetails(int id)
    {
        var result = await _assignmentService.GetApartmentTenantsAsync(id, landlordId: 0);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(ApartmentsOccupancy));
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> PendingRequests(
        int page = 1,
        int pageSize = 3,
        string? city = null,
        string? landlord = null,
        string sortBy = "createdat",
        bool desc = false)
    {
        ViewBag.CurrentSort = sortBy;
        ViewBag.IsDescending = desc;

        var result = await _requestService.GetPendingRequestsAsync(page, pageSize, city, landlord, sortBy, desc);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new PendingRequestsListViewModel());
        }

        return View(new PendingRequestsListViewModel
        {
            Requests = result.Data!.Items.Select(MapPendingRequest).ToList(),
            Pagination = BuildPagination(
                result.Data,
                nameof(PendingRequests),
                new Dictionary<string, string?>
                {
                    ["pageSize"] = pageSize.ToString(),
                    ["city"] = city,
                    ["landlord"] = landlord,
                    ["sortBy"] = sortBy,
                    ["desc"] = desc.ToString().ToLowerInvariant()
                }),
            CityFilter = city,
            LandlordFilter = landlord,
            SortBy = sortBy,
            Descending = desc
        });
    }

    [HttpGet]
    public async Task<IActionResult> ReviewRequest(int id)
    {
        var adminUserId = GetCurrentUserId();
        if (adminUserId is null)
        {
            return Challenge();
        }

        var result = await _requestService.GetRequestDetailsAsync(id, adminUserId.Value, requireAdmin: true);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(PendingRequests));
        }

        var request = result.Data!;
        var model = new ReviewRequestViewModel
        {
            RequestId = request.RequestId,
            Status = request.Status,
            Type = request.Type,
            Message = request.Message,
            CreatedAt = request.CreatedAt,
            ResolvedAt = request.ResolvedAt,
            LandlordName = request.LandlordName,
            LandlordEmail = request.LandlordEmail,
            LandlordPhoneNumber = request.LandlordPhoneNumber,
            Apartment = new ApartmentDetailsViewModel
            {
                ApartmentId = request.Apartment.ApartmentId,
                Address = request.Apartment.Address,
                City = request.Apartment.City,
                PricePerMonth = request.Apartment.PricePerMonth,
                TotalSeats = request.Apartment.TotalSeats,
                Amenities = request.Apartment.Amenities,
                VirtualTourUrl = request.Apartment.VirtualTourUrl,
                IsActive = request.Apartment.IsActive,
                LatestRequestStatus = request.Apartment.LatestRequestStatus,
                LatestRequestMessage = request.Apartment.LatestRequestMessage,
                LatestRequestDate = request.Apartment.LatestRequestDate,
                LatestResolvedAt = request.Apartment.LatestResolvedAt,
                Photos = request.Apartment.Photos.Select(photo => new ApartmentPhotoViewModel
                {
                    MediaId = photo.MediaId,
                    Url = photo.Url
                }).ToList()
            },
            RejectRequest = new RejectRequestViewModel
            {
                RequestId = request.RequestId
            }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(int id)
    {
        var adminUserId = GetCurrentUserId();
        if (adminUserId is null)
        {
            return Challenge();
        }

        var result = await _requestService.ApproveRequestAsync(id, adminUserId.Value);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Apartment request approved."
            : result.ErrorMessage ?? "Unable to approve request.";

        return RedirectToAction(nameof(PendingRequests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest([Bind(Prefix = "RejectRequest")] RejectRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Rejection reason is required and must be at least 10 characters.";
            return RedirectToAction(nameof(ReviewRequest), new { id = model.RequestId });
        }

        var adminUserId = GetCurrentUserId();
        if (adminUserId is null)
        {
            return Challenge();
        }

        var result = await _requestService.RejectRequestAsync(new AdminReviewRequestDto
        {
            RequestId = model.RequestId,
            Reason = model.Reason
        }, adminUserId.Value);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Apartment request rejected."
            : result.ErrorMessage ?? "Unable to reject request.";

        return RedirectToAction(result.Succeeded ? nameof(PendingRequests) : nameof(ReviewRequest), result.Succeeded ? null : new { id = model.RequestId });
    }

    [HttpGet]
    public async Task<IActionResult> RequestHistory(
        int page = 1,
        int pageSize = 3,
        string? status = null,
        string? city = null,
        string? landlord = null,
        string sortBy = "resolvedat",
        bool desc = true)
    {
        ViewBag.CurrentSort = sortBy;
        ViewBag.IsDescending = desc;

        var result = await _requestService.GetRequestHistoryAsync(page, pageSize, status, city, landlord, sortBy, desc);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new RequestHistoryViewModel());
        }

        return View(new RequestHistoryViewModel
        {
            Requests = result.Data!.Items.Select(MapPendingRequest).ToList(),
            Pagination = BuildPagination(
                result.Data,
                nameof(RequestHistory),
                new Dictionary<string, string?>
                {
                    ["pageSize"] = pageSize.ToString(),
                    ["status"] = status,
                    ["city"] = city,
                    ["landlord"] = landlord,
                    ["sortBy"] = sortBy,
                    ["desc"] = desc.ToString().ToLowerInvariant()
                }),
            StatusFilter = status,
            CityFilter = city,
            LandlordFilter = landlord,
            SortBy = sortBy,
            Descending = desc
        });
    }

    [HttpGet]
    public async Task<IActionResult> Landlords(
        int page = 1,
        int pageSize = 3,
        string? search = null,
        string? status = null,
        string sortBy = "registered",
        bool desc = true)
    {
        ViewBag.CurrentSort = sortBy;
        ViewBag.IsDescending = desc;

        var result = await _adminService.GetAllLandlordsAsync(page, pageSize, search, status, sortBy, desc);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new LandlordsViewModel());
        }

        return View(new LandlordsViewModel
        {
            Landlords = result.Data!.Items.Select(item => new LandlordListItemViewModel
            {
                LandlordId = item.LandlordId,
                Name = item.Name,
                Email = item.Email,
                PhoneNumber = item.PhoneNumber,
                VerificationStatus = item.VerificationStatus,
                TotalApartments = item.TotalApartments,
                ActiveApartments = item.ActiveApartments,
                Status = item.Status,
                RegistrationDate = item.RegistrationDate
            }).ToList(),
            Pagination = BuildPagination(
                result.Data,
                nameof(Landlords),
                new Dictionary<string, string?>
                {
                    ["pageSize"] = pageSize.ToString(),
                    ["search"] = search,
                    ["status"] = status,
                    ["sortBy"] = sortBy,
                    ["desc"] = desc.ToString().ToLowerInvariant()
                }),
            Search = search,
            StatusFilter = status,
            SortBy = sortBy,
            Descending = desc
        });
    }

    [HttpGet]
    public async Task<IActionResult> PendingLandlordVerifications(int page = 1, int pageSize = 10)
    {
        var result = await _adminService.GetPendingVerificationsAsync(page, pageSize);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new PagedResult<Sakanak.BLL.DTOs.Admin.LandlordVerificationRequestDto>());
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> LandlordVerificationHistory(int page = 1, int pageSize = 10, string? status = null)
    {
        var result = await _adminService.GetAllVerificationsAsync(page, pageSize, status);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new PagedResult<Sakanak.BLL.DTOs.Admin.LandlordVerificationRequestDto>());
        }

        ViewBag.StatusFilter = status;
        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> ReviewLandlord(int id)
    {
        var result = await _adminService.GetLandlordVerificationAsync(id);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(PendingLandlordVerifications));
        }

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveLandlord(int id)
    {
        var adminUserId = GetCurrentUserId();
        if (adminUserId is null)
        {
            return Challenge();
        }

        var result = await _adminService.ApproveLandlordAsync(id, adminUserId.Value);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Landlord verified successfully."
            : result.ErrorMessage ?? "Unable to verify landlord.";

        return RedirectToAction(nameof(PendingLandlordVerifications));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectLandlord(int id, string reason)
    {
        var adminUserId = GetCurrentUserId();
        if (adminUserId is null)
        {
            return Challenge();
        }

        var result = await _adminService.RejectLandlordAsync(id, adminUserId.Value, reason);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Landlord verification rejected."
            : result.ErrorMessage ?? "Unable to reject landlord verification.";

        return RedirectToAction(nameof(ReviewLandlord), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> LandlordDetails(int id)
    {
        var result = await _adminService.GetLandlordDetailsAsync(id);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Landlords));
        }

        var landlord = result.Data!;
        return View(new LandlordDetailsViewModel
        {
            LandlordId = landlord.LandlordId,
            Name = landlord.Name,
            Email = landlord.Email,
            PhoneNumber = landlord.PhoneNumber,
            Age = landlord.Age,
            VerificationStatus = landlord.VerificationStatus,
            Status = landlord.Status,
            RegistrationDate = landlord.RegistrationDate,
            TotalApartments = landlord.TotalApartments,
            ActiveApartments = landlord.ActiveApartments,
            Apartments = landlord.Apartments.Select(item => new LandlordApartmentViewModel
            {
                ApartmentId = item.ApartmentId,
                Address = item.Address,
                City = item.City,
                PricePerMonth = item.PricePerMonth,
                IsActive = item.IsActive,
                RequestStatus = item.RequestStatus
            }).ToList(),
            SuspendLandlord = new SuspendLandlordViewModel
            {
                LandlordId = landlord.LandlordId
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuspendLandlord([Bind(Prefix = "SuspendLandlord")] SuspendLandlordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Suspension reason is required and must be at least 10 characters.";
            return RedirectToAction(nameof(LandlordDetails), new { id = model.LandlordId });
        }

        var adminUserId = GetCurrentUserId();
        if (adminUserId is null)
        {
            return Challenge();
        }

        var result = await _adminService.SuspendLandlordAsync(model.LandlordId, adminUserId.Value, model.Reason);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Landlord suspended successfully."
            : result.ErrorMessage ?? "Unable to suspend landlord.";

        return RedirectToAction(nameof(LandlordDetails), new { id = model.LandlordId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateLandlord(int id)
    {
        var adminUserId = GetCurrentUserId();
        if (adminUserId is null)
        {
            return Challenge();
        }

        var result = await _adminService.ReactivateLandlordAsync(id, adminUserId.Value);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Landlord reactivated successfully."
            : result.ErrorMessage ?? "Unable to reactivate landlord.";

        return RedirectToAction(nameof(LandlordDetails), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Apartments(
        int page = 1,
        int pageSize = 3,
        string? city = null,
        string? landlord = null,
        string? status = null,
        string sortBy = "address",
        bool desc = false)
    {
        ViewBag.CurrentSort = sortBy;
        ViewBag.IsDescending = desc;

        var result = await _adminService.GetAllApartmentsAsync(page, pageSize, city, landlord, status, sortBy, desc);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new AdminApartmentsViewModel());
        }

        return View(new AdminApartmentsViewModel
        {
            Apartments = result.Data!.Items.Select(item => new AdminApartmentListItemViewModel
            {
                ApartmentId = item.ApartmentId,
                LandlordId = item.LandlordId,
                Address = item.Address,
                City = item.City,
                PricePerMonth = item.PricePerMonth,
                TotalSeats = item.TotalSeats,
                IsActive = item.IsActive,
                LandlordName = item.LandlordName,
                LandlordEmail = item.LandlordEmail,
                LatestRequestStatus = item.LatestRequestStatus,
                PrimaryPhotoUrl = item.PrimaryPhotoUrl
            }).ToList(),
            Pagination = BuildPagination(
                result.Data,
                nameof(Apartments),
                new Dictionary<string, string?>
                {
                    ["pageSize"] = pageSize.ToString(),
                    ["city"] = city,
                    ["landlord"] = landlord,
                    ["status"] = status,
                    ["sortBy"] = sortBy,
                    ["desc"] = desc.ToString().ToLowerInvariant()
                }),
            CityFilter = city,
            LandlordFilter = landlord,
            StatusFilter = status,
            SortBy = sortBy,
            Descending = desc
        });
    }

    [HttpGet]
    public async Task<IActionResult> ApartmentDetails(int id)
    {
        var result = await _adminService.GetApartmentDetailsAsync(id);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Apartments));
        }

        var apartment = result.Data!;
        return View(new AdminApartmentDetailsViewModel
        {
            ApartmentId = apartment.ApartmentId,
            LandlordId = apartment.LandlordId,
            Address = apartment.Address,
            City = apartment.City,
            PricePerMonth = apartment.PricePerMonth,
            TotalSeats = apartment.TotalSeats,
            Amenities = apartment.Amenities,
            VirtualTourUrl = apartment.VirtualTourUrl,
            IsActive = apartment.IsActive,
            LatestRequestStatus = apartment.LatestRequestStatus,
            LandlordName = apartment.LandlordName,
            LandlordEmail = apartment.LandlordEmail,
            LandlordPhoneNumber = apartment.LandlordPhoneNumber,
            Photos = apartment.Photos.Select(photo => new ApartmentPhotoViewModel
            {
                MediaId = photo.MediaId,
                Url = photo.Url
            }).ToList(),
            SuspendApartment = new SuspendApartmentViewModel
            {
                ApartmentId = apartment.ApartmentId
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuspendApartment([Bind(Prefix = "SuspendApartment")] SuspendApartmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Suspension reason is required and must be at least 10 characters.";
            return RedirectToAction(nameof(ApartmentDetails), new { id = model.ApartmentId });
        }

        var adminUserId = GetCurrentUserId();
        if (adminUserId is null)
        {
            return Challenge();
        }

        var result = await _adminService.SuspendApartmentAsync(model.ApartmentId, adminUserId.Value, model.Reason);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Apartment suspended successfully."
            : result.ErrorMessage ?? "Unable to suspend apartment.";

        return RedirectToAction(nameof(ApartmentDetails), new { id = model.ApartmentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateApartment(int id)
    {
        var adminUserId = GetCurrentUserId();
        if (adminUserId is null)
        {
            return Challenge();
        }

        var result = await _adminService.ReactivateApartmentAsync(id, adminUserId.Value);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Apartment reactivated successfully."
            : result.ErrorMessage ?? "Unable to reactivate apartment.";

        return RedirectToAction(nameof(ApartmentDetails), new { id });
    }

    private Guid? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }

    private static PendingRequestListItemViewModel MapPendingRequest(Sakanak.BLL.DTOs.Request.RequestListItemDto dto)
    {
        return new PendingRequestListItemViewModel
        {
            RequestId = dto.RequestId,
            ApartmentId = dto.ApartmentId,
            ApartmentAddress = dto.ApartmentAddress,
            City = dto.City,
            LandlordName = dto.LandlordName,
            LandlordEmail = dto.LandlordEmail,
            CreatedAt = dto.CreatedAt,
            ThumbnailUrl = dto.ThumbnailUrl,
            Status = dto.Status,
            Type = dto.Type
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
            Controller = "Admin",
            RouteValues = routeValues
        };
    }
}
