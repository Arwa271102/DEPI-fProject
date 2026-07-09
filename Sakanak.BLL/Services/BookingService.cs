using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Booking;
using Sakanak.BLL.Interfaces;
using Sakanak.BLL.Options;
using Sakanak.DAL.Data;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Services;

public class BookingService : IBookingService
{
    private const int MaxPendingBookings = 3;
    private const int MinimumNoticeDays = 7;
    private readonly SakanakDbContext _dbContext;
    private readonly IValidator<CreateBookingDto> _validator;
    private readonly IEmailService _emailService;
    private readonly BusinessRuleOptions _businessRules;
    private readonly INotificationService _notificationService;

    public BookingService(SakanakDbContext dbContext, IValidator<CreateBookingDto> validator, IEmailService emailService, IOptions<BusinessRuleOptions> businessRules, INotificationService notificationService)
    {
        _dbContext = dbContext;
        _validator = validator;
        _emailService = emailService;
        _businessRules = businessRules.Value;
        _notificationService = notificationService;
    }

    public async Task<Result<int>> CreateBookingAsync(int studentId, CreateBookingDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Result<int>.Failure(validation.Errors.Select(error => error.ErrorMessage));
        }

        var student = await _dbContext.Students
            .Include(item => item.ApplicationUser)
            .Include(item => item.Questionnaire)
            .Include(item => item.Bookings)
            .FirstOrDefaultAsync(item => item.StudentId == studentId);

        if (student is null)
        {
            return Result<int>.Failure("Student profile was not found.");
        }

        var canBook = ValidateStudentCanBook(student);
        if (!canBook.Succeeded)
        {
            return Result<int>.Failure(canBook.Errors);
        }

        var hasAcceptedBooking = await _dbContext.Bookings.AnyAsync(item =>
            item.StudentId == studentId && item.Status == BookingStatus.Accepted);
        if (hasAcceptedBooking)
        {
            return Result<int>.Failure("You already have an accepted booking. Cancel it first or wait for contract approval.");
        }

        var activeContract = await _dbContext.Contracts
            .Where(item => item.StudentId == studentId && item.Status == ContractStatus.Active)
            .OrderByDescending(item => item.EndDate)
            .FirstOrDefaultAsync();
        if (activeContract is not null)
        {
            return Result<int>.Failure($"You already have an active rental contract. Your current rental ends on {activeContract.EndDate:dd MMM yyyy}.");
        }

        var dateValidation = ValidateDates(dto.RequestedStartDate, dto.RequestedEndDate);
        if (!dateValidation.Succeeded)
        {
            return Result<int>.Failure(dateValidation.Errors);
        }

        var apartment = await BookableApartmentsQuery()
            .FirstOrDefaultAsync(item => item.ApartmentId == dto.ApartmentId);

        if (apartment is null)
        {
            return Result<int>.Failure("Apartment is not available for booking.");
        }

        var duplicateExists = await _dbContext.Bookings.AnyAsync(item =>
            item.StudentId == studentId &&
            item.ApartmentId == dto.ApartmentId &&
            (item.Status == BookingStatus.Pending || item.Status == BookingStatus.Accepted));

        if (duplicateExists)
        {
            return Result<int>.Failure("You already have an active booking request for this apartment.");
        }

        var availableSeats = await CalculateAvailableSeatsAsync(dto.ApartmentId, dto.RequestedStartDate, dto.RequestedEndDate);
        if (availableSeats <= 0)
        {
            return Result<int>.Failure("This apartment is fully booked for the selected dates.");
        }

        if (dto.ApartmentGroupId.HasValue)
        {
            var groupExists = await _dbContext.ApartmentGroups.AnyAsync(item =>
                item.GroupId == dto.ApartmentGroupId.Value &&
                item.ApartmentId == dto.ApartmentId);

            if (!groupExists)
            {
                return Result<int>.Failure("Selected apartment group was not found.");
            }
        }

        var booking = new Booking
        {
            StudentId = studentId,
            ApartmentId = dto.ApartmentId,
            ApartmentGroupId = dto.ApartmentGroupId,
            BookingDate = DateTime.UtcNow,
            RequestedStartDate = dto.RequestedStartDate.Date,
            RequestedEndDate = dto.RequestedEndDate.Date,
            Status = BookingStatus.Pending,
            Message = string.IsNullOrWhiteSpace(dto.Message) ? null : dto.Message.Trim(),
            PricePerMonthAtBooking = apartment.PricePerMonth,
            AddressAtBooking = apartment.Address,
            CityAtBooking = apartment.City,
            AmenitiesAtBooking = apartment.Amenities
        };

        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        await NotifyLandlordNewBookingAsync(apartment, student, booking);
        return Result<int>.Success(booking.BookingId);
    }

    public async Task<Result<List<BookingDto>>> GetStudentBookingsAsync(int studentId, BookingStatus? statusFilter = null)
    {
        var query = BookingDetailsQuery()
            .Where(item => item.StudentId == studentId);

        if (statusFilter.HasValue)
        {
            query = query.Where(item => item.Status == statusFilter.Value);
        }

        var bookings = await query
            .OrderByDescending(item => item.BookingDate)
            .ToListAsync();

        return Result<List<BookingDto>>.Success(bookings.Select(MapBooking).ToList());
    }

    public async Task<Result<BookingDetailsDto>> GetBookingDetailsAsync(int bookingId, int studentId)
    {
        var booking = await BookingDetailsQuery()
            .FirstOrDefaultAsync(item => item.BookingId == bookingId && item.StudentId == studentId);

        return booking is null
            ? Result<BookingDetailsDto>.Failure("Booking was not found.")
            : Result<BookingDetailsDto>.Success(await MapBookingDetailsAsync(booking));
    }

    public async Task<Result> CancelBookingAsync(int bookingId, int studentId)
    {
        var booking = await BookingDetailsQuery()
            .FirstOrDefaultAsync(item => item.BookingId == bookingId && item.StudentId == studentId);

        if (booking is null)
        {
            return Result.Failure("Booking was not found.");
        }

        if (booking.Status is not (BookingStatus.Pending or BookingStatus.Accepted))
        {
            return Result.Failure("Only pending or accepted bookings can be cancelled.");
        }

        var activeContract = await _dbContext.Contracts.AnyAsync(item =>
            item.BookingId == bookingId && item.Status == ContractStatus.Active);
        if (activeContract)
        {
            return Result.Failure("Cannot cancel booking with active paid contract. Please contact admin.");
        }

        var wasAccepted = booking.Status == BookingStatus.Accepted;
        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        booking.CancellationReason = wasAccepted
            ? "Student cancelled an accepted booking."
            : "Student cancelled the booking request.";

        // Auto-cancel associated contract if exists and active
        var contract = await _dbContext.Contracts
            .Include(c => c.Student).ThenInclude(s => s.ApplicationUser)
            .Include(c => c.Landlord).ThenInclude(l => l.ApplicationUser)
            .FirstOrDefaultAsync(c => c.BookingId == bookingId);

        if (contract is not null && (contract.Status == ContractStatus.PendingApproval || contract.Status == ContractStatus.Approved))
        {
            contract.Status = ContractStatus.Cancelled;
            contract.CancellationDate = DateTime.UtcNow;
            contract.CancellationReason = "Related booking was cancelled by student.";

            // Notify admin
            var admins = await _dbContext.Admins.Include(item => item.ApplicationUser).Where(item => item.ApplicationUser.Email != null).ToListAsync();
            foreach (var admin in admins)
            {
                await _emailService.SendEmailAsync(admin.ApplicationUser.Email!, "Contract auto-cancelled", $"<p>Contract #{contract.ContractId} was auto-cancelled because the student cancelled the associated booking request.</p>");
            }

            // Notify landlord
            if (!string.IsNullOrWhiteSpace(contract.Landlord.ApplicationUser.Email))
            {
                await _emailService.SendEmailAsync(contract.Landlord.ApplicationUser.Email!, "Associated contract cancelled", $"<p>Student {booking.Student.ApplicationUser.Name} cancelled their booking and the associated contract for your apartment at {booking.Apartment.Address}.</p>");
            }
        }

        await _dbContext.SaveChangesAsync();

        await NotifyLandlordBookingCancelledAsync(booking, wasAccepted);
        return Result.Success();
    }

    public async Task<Result<List<BookingDto>>> GetLandlordBookingsAsync(int landlordId, int? apartmentId = null, BookingStatus? statusFilter = null)
    {
        var query = BookingDetailsQuery()
            .Where(item => item.Apartment.LandlordId == landlordId);

        if (apartmentId.HasValue)
        {
            query = query.Where(item => item.ApartmentId == apartmentId.Value);
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(item => item.Status == statusFilter.Value);
        }

        var bookings = await query
            .OrderByDescending(item => item.BookingDate)
            .ToListAsync();

        return Result<List<BookingDto>>.Success(bookings.Select(MapBooking).ToList());
    }

    public async Task<Result<BookingDetailsDto>> GetBookingDetailsForLandlordAsync(int bookingId, int landlordId)
    {
        var booking = await BookingDetailsQuery()
            .FirstOrDefaultAsync(item => item.BookingId == bookingId && item.Apartment.LandlordId == landlordId);

        return booking is null
            ? Result<BookingDetailsDto>.Failure("Booking was not found.")
            : Result<BookingDetailsDto>.Success(await MapBookingDetailsAsync(booking));
    }

    public async Task<Result> AcceptBookingAsync(int bookingId, int landlordId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var booking = await BookingDetailsQuery()
            .FirstOrDefaultAsync(item => item.BookingId == bookingId && item.Apartment.LandlordId == landlordId);

        if (booking is null)
        {
            return Result.Failure("Booking was not found.");
        }

        if (booking.Status != BookingStatus.Pending)
        {
            return Result.Failure("Only pending bookings can be accepted.");
        }

        var availableSeats = await CalculateAvailableSeatsAsync(booking.ApartmentId, booking.RequestedStartDate, booking.RequestedEndDate);
        if (availableSeats <= 0)
        {
            return Result.Failure("There are no available seats for this booking period.");
        }

        booking.Status = BookingStatus.Accepted;
        booking.AcceptedAt = DateTime.UtcNow;
        var apartmentGroup = await _dbContext.ApartmentGroups
            .Include(g => g.Students)
            .FirstOrDefaultAsync(g => g.ApartmentId == booking.ApartmentId);

        if (apartmentGroup != null)
        {
            booking.ApartmentGroupId = apartmentGroup.GroupId;
            booking.Student.ApartmentGroupId = apartmentGroup.GroupId;
            
            // Check if group is now full (Current members + 1 for this new student)
            // Note: If Students collection doesn't include this student yet, add 1.
            var currentMemberCount = apartmentGroup.Students.Count;
            if (!apartmentGroup.Students.Any(s => s.StudentId == booking.StudentId))
            {
                currentMemberCount++;
            }

            if (currentMemberCount >= apartmentGroup.MaxMembers)
            {
                apartmentGroup.GroupStatus = GroupStatus.Full;
            }
        }

        var autoCancelledBookings = await AutoCancelOtherPendingBookingsAsync(booking.StudentId, booking.BookingId);
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        await NotifyStudentBookingAcceptedAsync(booking);
        foreach (var cancelledBooking in autoCancelledBookings)
        {
            await NotifyLandlordBookingAutoCancelledAsync(cancelledBooking);
        }

        if (autoCancelledBookings.Count > 0)
        {
            await NotifyStudentOtherBookingsCancelledAsync(booking, autoCancelledBookings);
        }

        if (apartmentGroup != null)
        {
            await NotifyRoommatesNewStudentJoinedAsync(booking, apartmentGroup);
        }

        return Result.Success();
    }

    public async Task<Result> RejectBookingAsync(int bookingId, int landlordId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
        {
            return Result.Failure("Rejection reason must be at least 10 characters.");
        }

        var booking = await BookingDetailsQuery()
            .FirstOrDefaultAsync(item => item.BookingId == bookingId && item.Apartment.LandlordId == landlordId);

        if (booking is null)
        {
            return Result.Failure("Booking was not found.");
        }

        if (booking.Status is not (BookingStatus.Pending or BookingStatus.Accepted))
        {
            return Result.Failure("Only pending or accepted bookings can be rejected.");
        }

        if (booking.Status == BookingStatus.Accepted && reason.Trim().Length < 20)
        {
            return Result.Failure("Accepted booking rejection reason must be at least 20 characters.");
        }

        booking.Status = BookingStatus.Rejected;
        booking.RejectedAt = DateTime.UtcNow;
        booking.RejectionReason = reason.Trim();

        if (booking.Contract is not null && booking.Contract.Status != ContractStatus.Rejected)
        {
            booking.Contract.Status = ContractStatus.Rejected;
            booking.Contract.ReviewedAt = DateTime.UtcNow;
            booking.Contract.RejectionReason = "Related booking was rejected by landlord.";
        }

        await _dbContext.SaveChangesAsync();

        await NotifyStudentBookingRejectedAsync(booking);
        if (booking.Contract is not null)
        {
            await NotifyStudentContractRejectedBecauseBookingRejectedAsync(booking);
        }

        return Result.Success();
    }

    public async Task<Result<int>> GetAvailableSeatsAsync(int apartmentId, DateTime startDate, DateTime endDate)
    {
        var apartmentExists = await _dbContext.Apartments.AnyAsync(item => item.ApartmentId == apartmentId);
        if (!apartmentExists)
        {
            return Result<int>.Failure("Apartment was not found.");
        }

        return Result<int>.Success(await CalculateAvailableSeatsAsync(apartmentId, startDate.Date, endDate.Date));
    }

    public async Task<Result<int>> GetAvailableSeatsAsync(int apartmentId, DateTime? forDate = null)
    {
        var apartmentExists = await _dbContext.Apartments.AnyAsync(item => item.ApartmentId == apartmentId);
        if (!apartmentExists)
        {
            return Result<int>.Failure("Apartment was not found.");
        }

        var date = forDate?.Date ?? DateTime.UtcNow.Date;
        var apartment = await _dbContext.Apartments.AsNoTracking().FirstAsync(item => item.ApartmentId == apartmentId);
        var occupiedSeats = await _dbContext.Bookings.CountAsync(item =>
            item.ApartmentId == apartmentId &&
            item.Status == BookingStatus.Accepted &&
            item.RequestedEndDate >= date);

        if (forDate.HasValue)
        {
            occupiedSeats = await _dbContext.Bookings.CountAsync(item =>
                item.ApartmentId == apartmentId &&
                item.Status == BookingStatus.Accepted &&
                item.RequestedStartDate <= date &&
                date <= item.RequestedEndDate);
        }

        return Result<int>.Success(Math.Max(0, apartment.TotalSeats - occupiedSeats));
    }

    public async Task<Result<bool>> CanStudentBookAsync(int studentId)
    {
        var student = await _dbContext.Students
            .Include(item => item.ApplicationUser)
            .Include(item => item.Questionnaire)
            .Include(item => item.Bookings)
            .FirstOrDefaultAsync(item => item.StudentId == studentId);

        if (student is null)
        {
            return Result<bool>.Failure("Student profile was not found.");
        }

        var validation = ValidateStudentCanBook(student);
        return validation.Succeeded
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(validation.Errors);
    }

    private IQueryable<Apartment> BookableApartmentsQuery()
        => _dbContext.Apartments
            .Include(item => item.Landlord)
                .ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Requests)
            .Include(item => item.Bookings)
            .Where(item =>
                item.IsActive &&
                item.Landlord.VerificationStatus &&
                item.Landlord.ApplicationUser.Status == UserStatus.Active &&
                item.Requests
                    .OrderByDescending(request => request.CreatedAt)
                    .Select(request => (RequestStatus?)request.Status)
                    .FirstOrDefault() == RequestStatus.Approved);

    private IQueryable<Booking> BookingDetailsQuery()
        => _dbContext.Bookings
            .Include(item => item.Student)
                .ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Student)
                .ThenInclude(item => item.Questionnaire)
            .Include(item => item.Student)
                .ThenInclude(item => item.Media)
            .Include(item => item.Contract)
            .Include(item => item.Apartment)
                .ThenInclude(item => item.Landlord)
                    .ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Apartment)
                .ThenInclude(item => item.Media);

    private static Result ValidateStudentCanBook(Student student)
    {
        if (!student.ApplicationUser.IsProfileComplete ||
            string.IsNullOrWhiteSpace(student.University) ||
            string.IsNullOrWhiteSpace(student.Faculty) ||
            student.Questionnaire is null)
        {
            return Result.Failure("Complete your profile and lifestyle questionnaire before booking.");
        }

        var pendingCount = student.Bookings.Count(item => item.Status == BookingStatus.Pending);
        return pendingCount >= MaxPendingBookings
            ? Result.Failure($"You can only have {MaxPendingBookings} pending bookings at a time.")
            : Result.Success();
    }

    private Result ValidateDates(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date;
            var minimumStart = DateTime.UtcNow.Date.AddDays(MinimumNoticeDays);

            if (start < minimumStart)
            {
                return Result.Failure($"Start date must be at least {MinimumNoticeDays} days from today.");
            }

            if (end <= start)
            {
                return Result.Failure("End date must be after the start date.");
            }

            if ((end - start).TotalDays < _businessRules.MinimumRentalDays)
            {
                return Result.Failure($"Minimum rental period is {_businessRules.MinimumRentalDays} days.");
            }

            return end > start.AddMonths(12)
                ? Result.Failure("Maximum booking duration is 12 months.")
                : Result.Success();
        }

    private async Task<int> CalculateAvailableSeatsAsync(int apartmentId, DateTime startDate, DateTime endDate)
    {
        var apartment = await _dbContext.Apartments
            .AsNoTracking()
            .FirstAsync(item => item.ApartmentId == apartmentId);

        var occupiedSeats = await _dbContext.Bookings.CountAsync(item =>
            item.ApartmentId == apartmentId &&
            item.Status == BookingStatus.Accepted &&
            item.RequestedStartDate < endDate.Date &&
            startDate.Date < item.RequestedEndDate);

        return Math.Max(0, apartment.TotalSeats - occupiedSeats);
    }

    private static BookingDto MapBooking(Booking booking)
        => new()
        {
            BookingId = booking.BookingId,
            ApartmentId = booking.ApartmentId,
            ApartmentAddress = string.IsNullOrWhiteSpace(booking.AddressAtBooking) ? booking.Apartment.Address : booking.AddressAtBooking,
            ApartmentCity = string.IsNullOrWhiteSpace(booking.CityAtBooking) ? booking.Apartment.City : booking.CityAtBooking,
            ApartmentPhotoUrl = booking.Apartment.Media
                .Where(item => item.Type == MediaType.Image)
                .OrderBy(item => item.MediaId)
                .Select(item => item.Url)
                .FirstOrDefault(),
            PricePerMonth = booking.PricePerMonthAtBooking > 0 ? booking.PricePerMonthAtBooking : booking.Apartment.PricePerMonth,
            CurrentPricePerMonth = booking.Apartment.PricePerMonth,
            AmenitiesAtBooking = booking.AmenitiesAtBooking.Length > 0 ? booking.AmenitiesAtBooking.ToList() : booking.Apartment.Amenities.ToList(),
            StudentName = booking.Student.ApplicationUser.Name,
            StudentPhotoUrl = booking.Student.Media
                .Where(item => item.EntityType == StudentProfileService.StudentEntityType && item.Type == MediaType.Image)
                .OrderByDescending(item => item.MediaId)
                .Select(item => item.Url)
                .FirstOrDefault(),
            LandlordName = booking.Apartment.Landlord.ApplicationUser.Name,
            BookingDate = booking.BookingDate,
            RequestedStartDate = booking.RequestedStartDate,
            RequestedEndDate = booking.RequestedEndDate,
            Status = booking.Status.ToString(),
            RejectionReason = booking.RejectionReason,
            CancellationReason = booking.CancellationReason,
            DaysUntilStart = Math.Max(0, (booking.RequestedStartDate.Date - DateTime.UtcNow.Date).Days)
        };

    private async Task<BookingDetailsDto> MapBookingDetailsAsync(Booking booking)
    {
        var summary = MapBooking(booking);
        var availableSeats = await CalculateAvailableSeatsAsync(booking.ApartmentId, booking.RequestedStartDate, booking.RequestedEndDate);
        var questionnaire = booking.Student.Questionnaire;

        return new BookingDetailsDto
        {
            BookingId = summary.BookingId,
            ApartmentId = summary.ApartmentId,
            ApartmentAddress = summary.ApartmentAddress,
            ApartmentCity = summary.ApartmentCity,
            ApartmentPhotoUrl = summary.ApartmentPhotoUrl,
            PricePerMonth = summary.PricePerMonth,
            StudentName = summary.StudentName,
            StudentPhotoUrl = summary.StudentPhotoUrl,
            LandlordName = summary.LandlordName,
            BookingDate = summary.BookingDate,
            RequestedStartDate = summary.RequestedStartDate,
            RequestedEndDate = summary.RequestedEndDate,
            Status = summary.Status,
            RejectionReason = summary.RejectionReason,
            CancellationReason = summary.CancellationReason,
            DaysUntilStart = summary.DaysUntilStart,
            StudentId = booking.StudentId,
            StudentApplicationUserId = booking.Student.ApplicationUserId,
            StudentEmail = booking.Student.ApplicationUser.Email ?? string.Empty,
            StudentPhone = booking.Student.ApplicationUser.PhoneNumber,
            StudentUniversity = booking.Student.University,
            StudentFaculty = booking.Student.Faculty,
            StudentAge = booking.Student.Age,
            Message = booking.Message,
            TotalSeats = booking.Apartment.TotalSeats,
            AvailableSeats = availableSeats,
            Amenities = summary.AmenitiesAtBooking,
            CurrentAmenities = booking.Apartment.Amenities.ToList(),
            PhotoUrls = booking.Apartment.Media
                .Where(item => item.Type == MediaType.Image)
                .OrderBy(item => item.MediaId)
                .Select(item => item.Url)
                .ToList(),
            SleepSchedule = questionnaire?.SleepSchedule.ToString(),
            IsSmoker = questionnaire?.IsSmoker,
            HygieneLevel = questionnaire?.HygieneLevel,
            StudyHabits = questionnaire?.StudyHabits.ToString(),
            SocialPreference = questionnaire?.SocialPreference.ToString(),
            GenderPreference = questionnaire?.GenderPreference.ToString(),
            AcceptedAt = booking.AcceptedAt,
            RejectedAt = booking.RejectedAt,
            CancelledAt = booking.CancelledAt,
            HasActiveContract = booking.Contract?.Status == ContractStatus.Active,
            IsInApartmentGroup = booking.ApartmentGroupId.HasValue || booking.Student.ApartmentGroupId.HasValue
        };
    }

    private async Task NotifyLandlordNewBookingAsync(Apartment apartment, Student student, Booking booking)
    {
        var landlordEmail = apartment.Landlord.ApplicationUser.Email;
        if (string.IsNullOrWhiteSpace(landlordEmail))
        {
            return;
        }

        await _emailService.SendEmailAsync(
            landlordEmail,
            $"New booking request for {apartment.Address}",
            $"<p>{student.ApplicationUser.Name} requested to book {apartment.Address} from {booking.RequestedStartDate:d} to {booking.RequestedEndDate:d}.</p>");
        await _notificationService.CreateNotificationAsync(
            apartment.Landlord.ApplicationUserId,
            "New Booking Request",
            $"{student.ApplicationUser.Name} wants to book {apartment.Address}.",
            NotificationType.NewBookingRequest,
            $"/Landlord/ReviewBooking/{booking.BookingId}");
    }

    private async Task NotifyStudentBookingAcceptedAsync(Booking booking)
    {
        var email = booking.Student.ApplicationUser.Email;
        if (!string.IsNullOrWhiteSpace(email))
        {
            await _emailService.SendEmailAsync(email, "Your booking request was accepted!", $"<p>Your booking for {booking.Apartment.Address} was accepted. Contract creation is next.</p>");
        }

        await _notificationService.CreateNotificationAsync(
            booking.Student.ApplicationUserId,
            "Booking Approved",
            $"Your booking for {booking.Apartment.Address} was accepted.",
            NotificationType.BookingApproved,
            $"/Student/BookingDetails/{booking.BookingId}");
    }

    private async Task NotifyStudentBookingRejectedAsync(Booking booking)
    {
        var email = booking.Student.ApplicationUser.Email;
        if (!string.IsNullOrWhiteSpace(email))
        {
            await _emailService.SendEmailAsync(email, "Booking request update", $"<p>Your booking for {booking.Apartment.Address} was rejected.</p><p>Reason: {booking.RejectionReason}</p>");
        }

        await _notificationService.CreateNotificationAsync(
            booking.Student.ApplicationUserId,
            "Booking Rejected",
            $"Your booking for {booking.Apartment.Address} was rejected.",
            NotificationType.BookingRejected,
            $"/Student/BookingDetails/{booking.BookingId}");
    }

    private async Task NotifyStudentContractRejectedBecauseBookingRejectedAsync(Booking booking)
    {
        var email = booking.Student.ApplicationUser.Email;
        if (!string.IsNullOrWhiteSpace(email))
        {
            await _emailService.SendEmailAsync(
                email,
                "Contract rejected because booking was rejected",
                $"<p>Your contract for {booking.Apartment.Address} was rejected because the related booking was rejected by the landlord.</p>");
        }
    }

    private async Task<List<Booking>> AutoCancelOtherPendingBookingsAsync(int studentId, int acceptedBookingId)
    {
        var otherPendingBookings = await BookingDetailsQuery()
            .Where(item =>
                item.StudentId == studentId &&
                item.BookingId != acceptedBookingId &&
                item.Status == BookingStatus.Pending)
            .ToListAsync();

        foreach (var booking in otherPendingBookings)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancellationReason = "Student accepted another booking.";
        }

        return otherPendingBookings;
    }

    private async Task NotifyLandlordBookingCancelledAsync(Booking booking, bool wasAccepted)
    {
        var email = booking.Apartment.Landlord.ApplicationUser.Email;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var subject = wasAccepted ? "Student cancelled accepted booking" : "Booking cancelled by student";
            await _emailService.SendEmailAsync(
                email,
                subject,
                $"<p>{booking.Student.ApplicationUser.Name} cancelled their booking for {booking.Apartment.Address}.</p><p>Dates: {booking.RequestedStartDate:d} to {booking.RequestedEndDate:d}</p>");
        }
    }

    private async Task NotifyLandlordBookingAutoCancelledAsync(Booking booking)
    {
        var email = booking.Apartment.Landlord.ApplicationUser.Email;
        if (!string.IsNullOrWhiteSpace(email))
        {
            await _emailService.SendEmailAsync(
                email,
                "Booking request auto-cancelled",
                $"<p>{booking.Student.ApplicationUser.Name}'s pending booking request for {booking.Apartment.Address} was cancelled because the student accepted another apartment.</p>");
        }
    }

    private async Task NotifyStudentOtherBookingsCancelledAsync(Booking acceptedBooking, IReadOnlyList<Booking> cancelledBookings)
    {
        var email = acceptedBooking.Student.ApplicationUser.Email;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var cancelledList = string.Join("<br>", cancelledBookings.Select(item => $"{item.Apartment.Address} ({item.RequestedStartDate:d} - {item.RequestedEndDate:d})"));
            await _emailService.SendEmailAsync(
                email,
                "Other pending bookings were cancelled",
                $"<p>Your booking for {acceptedBooking.Apartment.Address} was accepted, so your other pending requests were automatically cancelled:</p><p>{cancelledList}</p>");
        }
    }
    private async Task NotifyRoommatesNewStudentJoinedAsync(Booking booking, ApartmentGroup apartmentGroup)
    {
        foreach (var existingStudent in apartmentGroup.Students)
        {
            if (existingStudent.StudentId == booking.StudentId) continue;
            
            await _notificationService.CreateNotificationAsync(
                existingStudent.ApplicationUserId,
                "New Roommate Joined!",
                $"{booking.Student.ApplicationUser.Name} has joined your apartment group for {booking.Apartment.Address}.",
                NotificationType.RoommateJoined,
                "/Student/MyApartment");
        }
    }
}
