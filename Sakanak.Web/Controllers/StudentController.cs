using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sakanak.BLL.DTOs.Booking;
using Sakanak.BLL.DTOs.Common;
using Sakanak.BLL.DTOs.Contract;
using Sakanak.BLL.DTOs.Questionnaire;
using Sakanak.BLL.DTOs.Search;
using Sakanak.BLL.DTOs.Student;
using Sakanak.BLL.Interfaces;
using Sakanak.DAL.UnitOfWork;
using Sakanak.Domain.Enums;
using Sakanak.Web.Models.ViewModels.Shared;
using Sakanak.Web.Models.ViewModels.Student;

namespace Sakanak.Web.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStudentProfileService _studentProfileService;
    private readonly IQuestionnaireService _questionnaireService;
    private readonly IApartmentSearchService _apartmentSearchService;
    private readonly IBookingService _bookingService;
    private readonly IContractService _contractService;
    private readonly IPaymentService _paymentService;
    private readonly IApartmentAssignmentService _assignmentService;
    private readonly IMatchingService _matchingService;
    private readonly INotificationService _notificationService;
    private readonly IMessageService _messageService;
    private readonly Microsoft.AspNetCore.Identity.UserManager<Sakanak.Domain.Entities.ApplicationUser> _userManager;

    public StudentController(
        IUnitOfWork unitOfWork,
        IStudentProfileService studentProfileService,
        IQuestionnaireService questionnaireService,
        IApartmentSearchService apartmentSearchService,
        IBookingService bookingService,
        IContractService contractService,
        IPaymentService paymentService,
        IApartmentAssignmentService assignmentService,
        IMatchingService matchingService,
        INotificationService notificationService,
        IMessageService messageService,
        Microsoft.AspNetCore.Identity.UserManager<Sakanak.Domain.Entities.ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _studentProfileService = studentProfileService;
        _questionnaireService = questionnaireService;
        _apartmentSearchService = apartmentSearchService;
        _bookingService = bookingService;
        _contractService = contractService;
        _paymentService = paymentService;
        _assignmentService = assignmentService;
        _matchingService = matchingService;
        _notificationService = notificationService;
        _messageService = messageService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var profileResult = await _studentProfileService.GetProfileAsync(studentId.Value);
        var featuredResult = await _apartmentSearchService.SearchApartmentsAsync(new ApartmentSearchDto
        {
            PageNumber = 1,
            PageSize = 4,
            SortBy = "CreatedDate",
            Descending = true
        });
        var citiesResult = await _apartmentSearchService.GetAvailableCitiesAsync();

        if (!profileResult.Succeeded)
        {
            TempData["ErrorMessage"] = profileResult.ErrorMessage;
            return View(new StudentDashboardViewModel());
        }

        var bookingsResult = await _bookingService.GetStudentBookingsAsync(studentId.Value);
        var activeBookings = bookingsResult.Succeeded
            ? bookingsResult.Data!.Where(b => b.Status == BookingStatus.Pending.ToString() || b.Status == BookingStatus.Accepted.ToString()).ToList()
            : new List<BookingDto>();
        var contractsResult = await _contractService.GetStudentContractsAsync(studentId.Value);
        var contracts = contractsResult.Succeeded ? contractsResult.Data! : new List<ContractDto>();
        var paymentsResult = await _paymentService.GetStudentPaymentsAsync(studentId.Value);
        var payments = paymentsResult.Succeeded ? paymentsResult.Data! : new List<Sakanak.BLL.DTOs.Payment.PaymentDto>();
        var assignmentResult = await _assignmentService.GetStudentCurrentApartmentAsync(studentId.Value);

        var identityUserIdRaw = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        var identityUserId = identityUserIdRaw != null ? Guid.Parse(identityUserIdRaw) : Guid.Empty;
        var notificationsResult = await _notificationService.GetRecentNotificationsAsync(identityUserId, 3);
        var messagesResult = await _messageService.GetUserConversationsAsync(identityUserId);
        var adminUser = (await _userManager.GetUsersInRoleAsync("Admin")).FirstOrDefault();

        return View(new StudentDashboardViewModel
        {
            Profile = profileResult.Data!,
            FeaturedApartments = featuredResult.Succeeded ? featuredResult.Data!.Items : Array.Empty<ApartmentListItemDto>(),
            Cities = citiesResult.Succeeded ? citiesResult.Data! : Array.Empty<string>(),
            ActiveBookingsCount = activeBookings.Count,
            RecentBookings = activeBookings.Take(5).ToList(),
            RecentContracts = contracts.Take(3).ToList(),
            RecentPayments = payments.Take(3).ToList(),
            PendingContracts = contracts.Count(item => item.Status == ContractStatus.PendingApproval.ToString()),
            ApprovedContracts = contracts.Count(item => item.Status == ContractStatus.Approved.ToString()),
            RejectedContracts = contracts.Count(item => item.Status == ContractStatus.Rejected.ToString()),
            PendingPayments = payments.Count(item => item.Status == PaymentStatus.Pending.ToString()),
            PaidPayments = payments.Count(item => item.Status == PaymentStatus.Paid.ToString()),
            CurrentApartment = assignmentResult.Succeeded ? assignmentResult.Data : null,
            RecentNotifications = notificationsResult.Succeeded ? notificationsResult.Data! : new(),
            RecentMessages = messagesResult.Succeeded ? messagesResult.Data!.Take(3).ToList() : new(),
            AdminUserId = adminUser?.Id
        });
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _studentProfileService.GetProfileAsync(studentId.Value);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Dashboard));
        }

        return View(MapProfileViewModel(result.Data!));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(StudentProfileViewModel model)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _studentProfileService.UpdateProfileAsync(studentId.Value, new UpdateStudentProfileDto
        {
            Name = model.Name,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            University = model.University,
            Faculty = model.Faculty,
            ProfilePhoto = model.ProfilePhoto
        });

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            var reload = await _studentProfileService.GetProfileAsync(studentId.Value);
            if (reload.Succeeded)
            {
                var current = MapProfileViewModel(reload.Data!);
                model.ProfilePhotoUrl = current.ProfilePhotoUrl;
                model.CompletionStatus = current.CompletionStatus;
                model.CompletionPercentage = current.CompletionPercentage;
                model.MissingFields = current.MissingFields;
                model.QuestionnaireComplete = current.QuestionnaireComplete;
                model.Age = current.Age;
            }

            return View(model);
        }

        TempData["SuccessMessage"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadProfilePhoto(IFormFile profilePhoto)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _studentProfileService.UploadProfilePhotoAsync(studentId.Value, profilePhoto);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Profile photo updated."
            : result.ErrorMessage ?? "Unable to upload profile photo.";

        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProfilePhoto()
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _studentProfileService.DeleteProfilePhotoAsync(studentId.Value);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Profile photo removed."
            : result.ErrorMessage ?? "Unable to remove profile photo.";

        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public async Task<IActionResult> Questionnaire()
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _questionnaireService.GetQuestionnaireAsync(studentId.Value);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Dashboard));
        }

        var questionnaire = result.Data!;
        return View(new QuestionnaireViewModel
        {
            SleepSchedule = questionnaire.SleepSchedule,
            IsSmoker = questionnaire.IsSmoker,
            HygieneLevel = questionnaire.HygieneLevel,
            StudyHabits = questionnaire.StudyHabits,
            SocialPreference = questionnaire.SocialPreference,
            GenderPreference = questionnaire.GenderPreference,
            LastUpdated = questionnaire.LastUpdated
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Questionnaire(QuestionnaireViewModel model)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _questionnaireService.SaveQuestionnaireAsync(studentId.Value, new LifestyleQuestionnaireDto
        {
            SleepSchedule = model.SleepSchedule,
            IsSmoker = model.IsSmoker,
            HygieneLevel = model.HygieneLevel,
            StudyHabits = model.StudyHabits,
            SocialPreference = model.SocialPreference,
            GenderPreference = model.GenderPreference
        });

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        TempData["SuccessMessage"] = "Lifestyle questionnaire saved.";
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public async Task<IActionResult> SearchApartments(
        string? city = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int? minSeats = null,
        int? maxSeats = null,
        DateTime? availableFrom = null,
        List<string>? amenities = null,
        string sortBy = "CreatedDate",
        bool desc = true,
        int page = 1,
        int pageSize = 12)
    {
        pageSize = pageSize is 12 or 24 or 48 ? pageSize : 12;

        var search = new ApartmentSearchDto
        {
            City = city,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            MinSeats = minSeats,
            MaxSeats = maxSeats,
            AvailableFrom = availableFrom,
            Amenities = NormalizeAmenities(amenities),
            SortBy = sortBy,
            Descending = desc,
            PageNumber = page,
            PageSize = pageSize
        };

        var result = await _apartmentSearchService.SearchApartmentsAsync(search);
        var citiesResult = await _apartmentSearchService.GetAvailableCitiesAsync();
        var amenitiesResult = await _apartmentSearchService.GetAvailableAmenitiesAsync();

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            result = await _apartmentSearchService.SearchApartmentsAsync(new ApartmentSearchDto());
        }

        var pagedResult = result.Data ?? new PagedResult<ApartmentListItemDto>();
        return View(new ApartmentSearchViewModel
        {
            Search = search,
            Apartments = pagedResult.Items,
            TotalCount = pagedResult.TotalCount,
            AvailableCities = citiesResult.Succeeded ? citiesResult.Data! : Array.Empty<string>(),
            AvailableAmenities = amenitiesResult.Succeeded ? amenitiesResult.Data! : Array.Empty<string>(),
            Pagination = BuildPagination(
                pagedResult,
                nameof(SearchApartments),
                new Dictionary<string, string?>
                {
                    ["city"] = city,
                    ["minPrice"] = minPrice?.ToString(),
                    ["maxPrice"] = maxPrice?.ToString(),
                    ["minSeats"] = minSeats?.ToString(),
                    ["maxSeats"] = maxSeats?.ToString(),
                    ["availableFrom"] = availableFrom?.ToString("yyyy-MM-dd"),
                    ["sortBy"] = sortBy,
                    ["desc"] = desc.ToString().ToLowerInvariant(),
                    ["pageSize"] = pageSize.ToString(),
                    ["amenities"] = string.Join(",", NormalizeAmenities(amenities))
                })
        });
    }

    [HttpGet]
    public async Task<IActionResult> ApartmentDetails(int id)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _apartmentSearchService.GetApartmentDetailsAsync(id, studentId.Value);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(SearchApartments));
        }

        var roommates = new List<Sakanak.BLL.DTOs.Matching.RoommateMatchDto>();
        var matchingResult = await _matchingService.GetGroupCompatibilityForStudentAsync(studentId.Value, id);
        if (matchingResult.Succeeded)
        {
            roommates = matchingResult.Data ?? new List<Sakanak.BLL.DTOs.Matching.RoommateMatchDto>();
        }

        return View(new ApartmentDiscoveryDetailsViewModel
        {
            Apartment = result.Data!,
            CurrentRoommates = roommates
        });
    }

    [HttpGet]
    public async Task<IActionResult> BookApartment(int id)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var apartmentResult = await _apartmentSearchService.GetApartmentDetailsAsync(id, studentId.Value);
        if (!apartmentResult.Succeeded)
        {
            TempData["ErrorMessage"] = apartmentResult.ErrorMessage;
            return RedirectToAction(nameof(SearchApartments));
        }

        var canBook = await _bookingService.CanStudentBookAsync(studentId.Value);
        if (!canBook.Succeeded)
        {
            TempData["ErrorMessage"] = canBook.ErrorMessage;
            return RedirectToAction(nameof(ApartmentDetails), new { id });
        }

        return View(new BookApartmentViewModel
        {
            Apartment = apartmentResult.Data!,
            ApartmentId = id
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookApartment(BookApartmentViewModel model)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await PopulateBookApartmentModelAsync(model, studentId.Value);
            return View(model);
        }

        var result = await _bookingService.CreateBookingAsync(studentId.Value, new CreateBookingDto
        {
            ApartmentId = model.ApartmentId,
            RequestedStartDate = model.RequestedStartDate,
            RequestedEndDate = model.RequestedEndDate,
            Message = model.Message
        });

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            await PopulateBookApartmentModelAsync(model, studentId.Value);
            return View(model);
        }

        TempData["SuccessMessage"] = "Booking request sent to the landlord.";
        return RedirectToAction(nameof(MyBookings));
    }

    [HttpGet]
    public async Task<IActionResult> MyBookings(BookingStatus? status = null)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _bookingService.GetStudentBookingsAsync(studentId.Value, status);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new MyBookingsViewModel());
        }

        return View(new MyBookingsViewModel
        {
            Bookings = result.Data!,
            StatusFilter = status
        });
    }

    [HttpGet]
    public async Task<IActionResult> BookingDetails(int id)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _bookingService.GetBookingDetailsAsync(id, studentId.Value);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(MyBookings));
        }

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _bookingService.CancelBookingAsync(id, studentId.Value);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Booking request cancelled."
            : result.ErrorMessage ?? "Unable to cancel booking.";

        return RedirectToAction(nameof(MyBookings));
    }

    [HttpGet]
    public async Task<IActionResult> CreateContract(int bookingId)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var bookingResult = await _bookingService.GetBookingDetailsAsync(bookingId, studentId.Value);
        if (!bookingResult.Succeeded)
        {
            TempData["ErrorMessage"] = bookingResult.ErrorMessage;
            return RedirectToAction(nameof(MyBookings));
        }

        var canCreate = await _contractService.CanCreateContractForBookingAsync(bookingId, studentId.Value);
        if (!canCreate.Succeeded)
        {
            TempData["ErrorMessage"] = canCreate.ErrorMessage;
            return RedirectToAction(nameof(BookingDetails), new { id = bookingId });
        }

        return View(new CreateContractViewModel
        {
            BookingId = bookingId,
            Booking = bookingResult.Data!,
            StartDate = bookingResult.Data!.RequestedStartDate,
            EndDate = bookingResult.Data.RequestedEndDate
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateContract(CreateContractViewModel model)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _contractService.CreateContractAsync(studentId.Value, new CreateContractDto
        {
            BookingId = model.BookingId,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            ContractDocument = model.ContractDocument,
            IdFrontPhoto = model.IdFrontPhoto,
            IdBackPhoto = model.IdBackPhoto,
            StudentIdCardPhoto = model.StudentIdCardPhoto,
            SupportingDocuments = model.SupportingDocuments,
            AcceptTerms = model.AcceptTerms
        });

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error);
            var bookingResult = await _bookingService.GetBookingDetailsAsync(model.BookingId, studentId.Value);
            model.Booking = bookingResult.Data;
            return View(model);
        }

        TempData["SuccessMessage"] = "Contract submitted for admin review.";
        return RedirectToAction(nameof(MyContracts));
    }

    [HttpGet]
    public async Task<IActionResult> MyContracts(ContractStatus? status = null)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _contractService.GetStudentContractsAsync(studentId.Value, status);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new List<ContractDto>());
        }

        ViewBag.StatusFilter = status;
        return View(result.Data!);
    }

    [HttpGet]
    public async Task<IActionResult> ContractDetails(int id)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _contractService.GetContractDetailsAsync(id, studentId.Value);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(MyContracts));
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> PayContract(int contractId)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var paymentResult = await _paymentService.GetPaymentForContractAsync(contractId, studentId.Value);
        if (!paymentResult.Succeeded)
        {
            TempData["ErrorMessage"] = paymentResult.ErrorMessage;
            return RedirectToAction(nameof(ContractDetails), new { id = contractId });
        }

        return View(paymentResult.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InitiatePayment(int paymentId)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var successUrl = $"{Request.Scheme}://{Request.Host}{Url.Action(nameof(PaymentSuccess), "Student")}?session_id={{CHECKOUT_SESSION_ID}}";
        var cancelUrl = $"{Request.Scheme}://{Request.Host}{Url.Action(nameof(PaymentCancelled), "Student")}";
        var result = await _paymentService.CreateCheckoutSessionAsync(paymentId, studentId.Value, successUrl, cancelUrl);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(MyPayments));
        }

        return Redirect(result.Data!);
    }

    [HttpGet]
    public async Task<IActionResult> PaymentSuccess(string? session_id)
    {
        var result = await _paymentService.VerifyAndCompletePaymentAsync(session_id ?? string.Empty);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(MyPayments));
        }

        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is not null)
        {
            var assignmentResult = await _assignmentService.GetStudentCurrentApartmentAsync(studentId.Value);
            ViewBag.Assignment = assignmentResult.Succeeded ? assignmentResult.Data : null;
        }
        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> MyApartment()
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _assignmentService.GetStudentCurrentApartmentAsync(studentId.Value);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Dashboard));
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> MyRoommates()
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _matchingService.GetMyGroupAsync(studentId.Value);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Dashboard));
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> FutureRoommates(int apartmentId, int bookingId)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var matchesResult = await _matchingService.GetGroupCompatibilityForStudentAsync(studentId.Value, apartmentId);
        if (!matchesResult.Succeeded)
        {
            TempData["ErrorMessage"] = matchesResult.ErrorMessage ?? "Unable to load future roommates.";
            return RedirectToAction(nameof(MyBookings));
        }

        ViewBag.ApartmentId = apartmentId;
        ViewBag.BookingId = bookingId;
        return View(matchesResult.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ProceedToContract(int bookingId)
    {
        return RedirectToAction(nameof(CreateContract), new { bookingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelDueToIncompatibility(int bookingId)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _bookingService.CancelBookingAsync(bookingId, studentId.Value);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Booking cancelled due to incompatibility."
            : result.ErrorMessage ?? "Unable to cancel booking.";

        return RedirectToAction(nameof(MyBookings));
    }

    [HttpGet]
    public IActionResult PaymentCancelled()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> MyPayments()
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _paymentService.GetStudentPaymentsAsync(studentId.Value);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return View(new List<Sakanak.BLL.DTOs.Payment.PaymentDto>());
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> PaymentDetails(int id)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _paymentService.GetPaymentDetailsAsync(id, studentId.Value);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(MyPayments));
        }

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelContract(int id)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _contractService.CancelContractAsync(id, studentId.Value);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Contract cancelled successfully."
            : result.ErrorMessage ?? "Unable to cancel contract.";

        return RedirectToAction(nameof(ContractDetails), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> EditContract(int id)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var contractResult = await _contractService.GetContractDetailsAsync(id, studentId.Value);
        if (!contractResult.Succeeded || contractResult.Data!.Status != ContractStatus.Rejected.ToString())
        {
            TempData["ErrorMessage"] = contractResult.ErrorMessage ?? "Only rejected contracts can be resubmitted.";
            return RedirectToAction(nameof(MyContracts));
        }

        var bookingResult = await _bookingService.GetBookingDetailsAsync(contractResult.Data.BookingId, studentId.Value);
        return View("CreateContract", new CreateContractViewModel
        {
            ContractId = id,
            BookingId = contractResult.Data.BookingId,
            Booking = bookingResult.Data,
            StartDate = contractResult.Data.StartDate,
            EndDate = contractResult.Data.EndDate
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditContract(int id, CreateContractViewModel model)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _contractService.UpdateContractAsync(id, studentId.Value, new CreateContractDto
        {
            BookingId = model.BookingId,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            ContractDocument = model.ContractDocument,
            IdFrontPhoto = model.IdFrontPhoto,
            IdBackPhoto = model.IdBackPhoto,
            StudentIdCardPhoto = model.StudentIdCardPhoto,
            SupportingDocuments = model.SupportingDocuments,
            AcceptTerms = model.AcceptTerms
        });

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error);
            var bookingResult = await _bookingService.GetBookingDetailsAsync(model.BookingId, studentId.Value);
            model.ContractId = id;
            model.Booking = bookingResult.Data;
            return View("CreateContract", model);
        }

        TempData["SuccessMessage"] = "Contract resubmitted for admin review.";
        return RedirectToAction(nameof(ContractDetails), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadContractTemplate(int bookingId)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var bookingResult = await _bookingService.GetBookingDetailsAsync(bookingId, studentId.Value);
        if (!bookingResult.Succeeded)
        {
            TempData["ErrorMessage"] = bookingResult.ErrorMessage;
            return RedirectToAction(nameof(MyBookings));
        }

        var pdf = GenerateContractTemplatePdf(bookingResult.Data!);
        return File(pdf, "application/pdf", $"contract-template-booking-{bookingId}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadContractDocument(int id)
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _contractService.GetContractDetailsAsync(id, studentId.Value);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Data!.DocumentUrl))
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Contract document was not found.";
            return RedirectToAction(nameof(MyContracts));
        }

        return Redirect(Url.Content(result.Data.DocumentUrl));
    }

    private async Task<int?> GetCurrentStudentIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var applicationUserId))
        {
            return null;
        }

        var student = await _unitOfWork.Students.GetByUserIdAsync(applicationUserId);
        return student?.StudentId;
    }

    private static List<string> NormalizeAmenities(List<string>? amenities)
        => (amenities ?? new List<string>())
            .SelectMany(item => item.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static StudentProfileViewModel MapProfileViewModel(StudentProfileDto dto)
        => new()
        {
            StudentId = dto.StudentId,
            Name = dto.Name,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            University = dto.University,
            Faculty = dto.Faculty,
            Age = dto.Age,
            ProfilePhotoUrl = dto.ProfilePhotoUrl,
            QuestionnaireComplete = dto.QuestionnaireComplete,
            CompletionStatus = dto.Completion.Status,
            CompletionPercentage = dto.Completion.CompletionPercentage,
            MissingFields = dto.Completion.MissingFields
        };

    private async Task PopulateBookApartmentModelAsync(BookApartmentViewModel model, int studentId)
    {
        var apartmentResult = await _apartmentSearchService.GetApartmentDetailsAsync(model.ApartmentId, studentId);
        if (apartmentResult.Succeeded)
        {
            model.Apartment = apartmentResult.Data!;
        }
    }

    private static PaginationViewModel BuildPagination<T>(
        PagedResult<T> pagedResult,
        string action,
        Dictionary<string, string?> routeValues)
        => new()
        {
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalCount = pagedResult.TotalCount,
            TotalPages = pagedResult.TotalPages,
            HasPrevious = pagedResult.HasPrevious,
            HasNext = pagedResult.HasNext,
            Action = action,
            Controller = "Student",
            RouteValues = routeValues
        };

    private static byte[] GenerateContractTemplatePdf(BookingDetailsDto booking)
    {
        var rawLines = new List<string>
        {
            "Sakanak Student Housing Rental Agreement",
            "",
            $"Generated: {DateTime.UtcNow:yyyy-MM-dd}",
            $"Booking ID: {booking.BookingId}",
            "",
            "Parties",
            $"Student: {booking.StudentName}",
            $"University / Faculty: {booking.StudentUniversity} - {booking.StudentFaculty}",
            $"Landlord: {booking.LandlordName}",
            "",
            "Property Details",
            $"Address: {booking.ApartmentAddress}, {booking.ApartmentCity}",
            $"Monthly Rent: {booking.PricePerMonth:C0}",
            $"Rental Period: {booking.RequestedStartDate:yyyy-MM-dd} to {booking.RequestedEndDate:yyyy-MM-dd}",
            "",
            "Terms and Conditions",
            "The student and landlord agree to follow Sakanak platform rules, provide truthful documentation, respect the property, maintain timely payment obligations, and report any material changes affecting this rental agreement.",
            "This template must be printed or digitally signed by both parties before upload. Admin review verifies document completeness, identity documents, signatures, and date consistency.",
            "",
            "Signatures",
            "Student Signature: ________________________________ Date: ______________",
            "Landlord Signature: _______________________________ Date: ______________"
        };

        var lines = rawLines.SelectMany(line => WrapPdfLine(line, 88)).Take(34).ToList();
        var textCommands = string.Join("\n", lines.Select((line, index) => $"1 0 0 1 50 {740 - (index * 20)} Tm ({EscapePdfText(line)}) Tj"));
        var pageContent = $"BT /F1 10 Tf\n{textCommands}\nET";
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {System.Text.Encoding.ASCII.GetByteCount(pageContent)} >>\nstream\n{pageContent}\nendstream"
        };

        var builder = new System.Text.StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        foreach (var obj in objects.Select((value, index) => new { value, number = index + 1 }))
        {
            offsets.Add(System.Text.Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append($"{obj.number} 0 obj\n{obj.value}\nendobj\n");
        }

        var xrefOffset = System.Text.Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 6\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append($"{offset:0000000000} 00000 n \n");
        }

        builder.Append($"trailer << /Root 1 0 R /Size 6 >>\nstartxref\n{xrefOffset}\n%%EOF");
        return System.Text.Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static string EscapePdfText(string value)
        => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static IEnumerable<string> WrapPdfLine(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield return string.Empty;
            yield break;
        }

        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var current = string.Empty;
        foreach (var word in words)
        {
            if (current.Length + word.Length + 1 > maxLength)
            {
                yield return current;
                current = word;
            }
            else
            {
                current = string.IsNullOrEmpty(current) ? word : $"{current} {word}";
            }
        }

        if (!string.IsNullOrEmpty(current))
        {
            yield return current;
        }
    }

    [HttpGet]
    public async Task<IActionResult> FindRoommates()
    {
        var studentId = await GetCurrentStudentIdAsync();
        if (studentId is null)
        {
            return Challenge();
        }

        var result = await _matchingService.FindCompatibleStudentsAsync(studentId.Value);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Questionnaire));
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> ViewRoommateProfile(int studentId)
    {
        var currentStudentId = await GetCurrentStudentIdAsync();
        if (currentStudentId is null)
        {
            return Challenge();
        }

        var result = await _matchingService.FindCompatibleStudentsAsync(currentStudentId.Value);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Questionnaire));
        }

        var match = result.Data!.FirstOrDefault(item => item.StudentId == studentId);
        if (match is null)
        {
            TempData["ErrorMessage"] = "Student profile not found or is not compatible.";
            return RedirectToAction(nameof(FindRoommates));
        }

        var questionnaireResult = await _questionnaireService.GetQuestionnaireAsync(studentId);
        ViewBag.Questionnaire = questionnaireResult.Succeeded ? questionnaireResult.Data : null;

        return View(match);
    }
}
