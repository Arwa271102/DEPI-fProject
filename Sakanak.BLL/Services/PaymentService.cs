using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Common;
using Sakanak.BLL.DTOs.Payment;
using Sakanak.BLL.Interfaces;
using Sakanak.BLL.Options;
using Sakanak.DAL.Data;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Services;

public class PaymentService : IPaymentService
{
    private readonly SakanakDbContext _dbContext;
    private readonly IStripeService _stripeService;
    private readonly IEmailService _emailService;
    private readonly StripeSettings _stripeSettings;
    private readonly ILogger<PaymentService> _logger;
    private readonly INotificationService _notificationService;

    public PaymentService(
        SakanakDbContext dbContext,
        IStripeService stripeService,
        IEmailService emailService,
        IOptions<StripeSettings> stripeSettings,
        ILogger<PaymentService> logger,
        INotificationService notificationService)
    {
        _dbContext = dbContext;
        _stripeService = stripeService;
        _emailService = emailService;
        _stripeSettings = stripeSettings.Value;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<Result<int>> GeneratePaymentForContractAsync(int contractId)
    {
        var existing = await _dbContext.Payments.FirstOrDefaultAsync(item => item.ContractId == contractId);
        if (existing is not null)
        {
            if (existing.Status == PaymentStatus.Pending)
            {
                var existingContract = await PaymentContractQuery().FirstOrDefaultAsync(item => item.ContractId == contractId);
                if (existingContract is not null)
                {
                    existing.Amount = CalculateAmount(existingContract);
                    await _dbContext.SaveChangesAsync();
                }
            }

            return Result<int>.Success(existing.PaymentId);
        }

        var contract = await PaymentContractQuery().FirstOrDefaultAsync(item => item.ContractId == contractId);
        if (contract is null)
        {
            return Result<int>.Failure("Contract was not found.");
        }

        if (contract.Status != ContractStatus.Approved)
        {
            return Result<int>.Failure("Payments can only be generated for approved contracts.");
        }

        var payment = new Payment
        {
            ContractId = contract.ContractId,
            StudentId = contract.StudentId,
            LandlordId = contract.LandlordId,
            ApartmentId = contract.ApartmentId,
            Amount = CalculateAmount(contract),
            CreatedAt = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(7),
            Status = PaymentStatus.Pending
        };

        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();
        return Result<int>.Success(payment.PaymentId);
    }

    public async Task<Result<PaymentDto>> GetPaymentForContractAsync(int contractId, int studentId)
    {
        var payment = await PaymentQuery().FirstOrDefaultAsync(item => item.ContractId == contractId && item.StudentId == studentId);
        if (payment is null)
        {
            return Result<PaymentDto>.Failure("Payment was not found.");
        }

        await RefreshPendingPaymentAmountAsync(payment);
        return Result<PaymentDto>.Success(MapPayment(payment));
    }

    public async Task<Result<string>> CreateCheckoutSessionAsync(int paymentId, int studentId, string successUrl, string cancelUrl)
    {
        var payment = await PaymentQuery().FirstOrDefaultAsync(item => item.PaymentId == paymentId && item.StudentId == studentId);
        if (payment is null)
        {
            return Result<string>.Failure("Payment was not found.");
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return Result<string>.Failure("This payment is not pending.");
        }

        try
        {
            await RefreshPendingPaymentAmountAsync(payment);

            if (!string.IsNullOrWhiteSpace(payment.StripeSessionId) && await _stripeService.VerifySessionPaidAsync(payment.StripeSessionId))
            {
                var groupValidation = await ValidateGroupBeforeActivationAsync(payment);
                if (!groupValidation.Succeeded)
                {
                    return Result<string>.Failure(groupValidation.Errors);
                }

                await CompletePaymentAsync(payment, payment.StripeSessionId);
                var verifiedSuccessUrl = successUrl.Replace("{CHECKOUT_SESSION_ID}", payment.StripeSessionId, StringComparison.Ordinal);
                return Result<string>.Success(verifiedSuccessUrl);
            }

            var description = $"Rental payment for {payment.Apartment.Address} ({payment.Contract.StartDate:d} - {payment.Contract.EndDate:d})";
            var session = await _stripeService.CreateCheckoutSessionAsync(payment.PaymentId, payment.Amount, payment.Student.ApplicationUser.Email ?? string.Empty, description, successUrl, cancelUrl);
            payment.StripeSessionId = session.SessionId;
            await _dbContext.SaveChangesAsync();
            return Result<string>.Success(session.CheckoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not create Stripe checkout session for payment {PaymentId}", paymentId);
            return Result<string>.Failure("Payment system is unavailable. Please try again.");
        }
    }

    public async Task<Result<PaymentDetailsDto>> VerifyAndCompletePaymentAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Result<PaymentDetailsDto>.Failure("Stripe session id is missing.");
        }

        var payment = await PaymentQuery().FirstOrDefaultAsync(item => item.StripeSessionId == sessionId);
        if (payment is null)
        {
            return Result<PaymentDetailsDto>.Failure("Payment session was not found.");
        }

        if (payment.Status == PaymentStatus.Paid)
        {
            return Result<PaymentDetailsDto>.Success(MapPaymentDetails(payment));
        }

        try
        {
            var paid = await _stripeService.VerifySessionPaidAsync(sessionId);
            if (!paid)
            {
                return Result<PaymentDetailsDto>.Failure("Payment was not completed.");
            }

            var groupValidation = await ValidateGroupBeforeActivationAsync(payment);
            if (!groupValidation.Succeeded)
            {
                return Result<PaymentDetailsDto>.Failure(groupValidation.Errors);
            }

            await CompletePaymentAsync(payment, sessionId);
            return Result<PaymentDetailsDto>.Success(MapPaymentDetails(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment verification failed for Stripe session {SessionId}", sessionId);
            return Result<PaymentDetailsDto>.Failure("Payment verification failed. Please contact support.");
        }
    }

    public async Task<Result<List<PaymentDto>>> GetStudentPaymentsAsync(int studentId)
        => Result<List<PaymentDto>>.Success((await PaymentQuery().Where(item => item.StudentId == studentId).OrderByDescending(item => item.CreatedAt).ToListAsync()).Select(MapPayment).ToList());

    public async Task<Result<List<PaymentDto>>> GetLandlordPaymentsAsync(int landlordId)
        => Result<List<PaymentDto>>.Success((await PaymentQuery().Where(item => item.LandlordId == landlordId).OrderByDescending(item => item.CreatedAt).ToListAsync()).Select(MapPayment).ToList());

    public async Task<Result<PaymentDetailsDto>> GetPaymentDetailsAsync(int paymentId, int userId)
    {
        var payment = await PaymentQuery().FirstOrDefaultAsync(item => item.PaymentId == paymentId && (item.StudentId == userId || item.LandlordId == userId));
        return payment is null ? Result<PaymentDetailsDto>.Failure("Payment was not found.") : Result<PaymentDetailsDto>.Success(MapPaymentDetails(payment));
    }

    public async Task<Result<PaymentDetailsDto>> GetPaymentDetailsForAdminAsync(int paymentId)
    {
        var payment = await PaymentQuery().FirstOrDefaultAsync(item => item.PaymentId == paymentId);
        return payment is null ? Result<PaymentDetailsDto>.Failure("Payment was not found.") : Result<PaymentDetailsDto>.Success(MapPaymentDetails(payment));
    }

    public async Task<Result<PagedResult<PaymentDto>>> GetAllPaymentsAsync(int page, int pageSize, PaymentStatus? statusFilter = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = PaymentQuery();
        if (statusFilter.HasValue)
        {
            query = query.Where(item => item.Status == statusFilter.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(item => item.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Result<PagedResult<PaymentDto>>.Success(new PagedResult<PaymentDto>
        {
            Items = items.Select(MapPayment).ToList(),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        });
    }

    private IQueryable<Payment> PaymentQuery()
        => _dbContext.Payments
            .Include(item => item.Student).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Landlord).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Apartment)
            .Include(item => item.Contract).ThenInclude(item => item.Booking);

    private IQueryable<Contract> PaymentContractQuery()
        => _dbContext.Contracts
            .Include(item => item.Student).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Landlord).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Apartment)
            .Include(item => item.Booking);

    private static decimal CalculateAmount(Contract contract)
    {
        var months = CalculateMonths(contract.StartDate, contract.EndDate);
        var monthlyRate = contract.Booking.PricePerMonthAtBooking > 0 ? contract.Booking.PricePerMonthAtBooking : contract.Apartment.PricePerMonth;
        return monthlyRate * months;
    }

    private static int CalculateMonths(DateTime start, DateTime end)
    {
        var months = ((end.Year - start.Year) * 12) + end.Month - start.Month;
        if (end.Day > start.Day) months++;
        return Math.Max(1, months);
    }

    private async Task AssignStudentToApartmentAsync(int studentId, int apartmentId)
    {
        var student = await _dbContext.Students.FirstAsync(item => item.StudentId == studentId);
        if (student.ApartmentGroupId.HasValue) return;

        var group = await _dbContext.ApartmentGroups.FirstOrDefaultAsync(item => item.ApartmentId == apartmentId && item.GroupStatus == GroupStatus.Open);
        if (group is null)
        {
            group = new ApartmentGroup { ApartmentId = apartmentId, MaxMembers = 1, GroupStatus = GroupStatus.Open };
            _dbContext.ApartmentGroups.Add(group);
            await _dbContext.SaveChangesAsync();
        }

        student.ApartmentGroupId = group.GroupId;
        group.GroupStatus = GroupStatus.Full;
    }

    private async Task CompletePaymentAsync(Payment payment, string sessionId)
    {
        payment.Status = PaymentStatus.Paid;
        payment.PaymentDate = DateTime.UtcNow;
        payment.PaidAt = DateTime.UtcNow;
        payment.StripePaymentIntentId = await _stripeService.GetPaymentIntentIdAsync(sessionId);
        payment.Contract.Status = ContractStatus.Active;
        payment.Contract.ActivatedAt = DateTime.UtcNow;
        await AssignStudentToApartmentAsync(payment.StudentId, payment.ApartmentId);
        await _dbContext.SaveChangesAsync();
        await NotifyPaymentSuccessAsync(payment);
    }

    private async Task<Result> ValidateGroupBeforeActivationAsync(Payment payment)
    {
        if (payment.Apartment.TotalSeats <= 1)
        {
            return Result.Success();
        }

        var studentGroupId = await _dbContext.Students
            .Where(item => item.StudentId == payment.StudentId)
            .Select(item => item.ApartmentGroupId)
            .FirstOrDefaultAsync();

        if (!studentGroupId.HasValue)
        {
            return Result.Failure("You must join an apartment group before paying for a shared apartment.");
        }

        var validGroup = await _dbContext.ApartmentGroups.AnyAsync(item =>
            item.GroupId == studentGroupId.Value &&
            item.ApartmentId == payment.ApartmentId &&
            item.GroupStatus != GroupStatus.Closed);

        return validGroup ? Result.Success() : Result.Failure("Your apartment group is not valid for this apartment.");
    }

    private async Task RefreshPendingPaymentAmountAsync(Payment payment)
    {
        if (payment.Status != PaymentStatus.Pending)
        {
            return;
        }

        var recalculatedAmount = CalculateAmount(payment.Contract);
        if (payment.Amount == recalculatedAmount)
        {
            return;
        }

        payment.Amount = recalculatedAmount;
        await _dbContext.SaveChangesAsync();
    }

    private static PaymentDto MapPayment(Payment payment)
        => new()
        {
            PaymentId = payment.PaymentId,
            ContractId = payment.ContractId,
            Amount = payment.Amount,
            DueDate = payment.DueDate,
            PaymentDate = payment.PaymentDate,
            PaidAt = payment.PaidAt,
            Status = payment.Status.ToString(),
            IsLate = payment.IsLate,
            CanPay = payment.Status == PaymentStatus.Pending,
            StudentName = payment.Student.ApplicationUser.Name,
            StudentEmail = payment.Student.ApplicationUser.Email ?? string.Empty,
            ApartmentAddress = payment.Apartment.Address,
            LandlordName = payment.Landlord.ApplicationUser.Name
        };

    private static PaymentDetailsDto MapPaymentDetails(Payment payment)
    {
        var dto = new PaymentDetailsDto();
        var summary = MapPayment(payment);
        dto.PaymentId = summary.PaymentId;
        dto.ContractId = summary.ContractId;
        dto.Amount = summary.Amount;
        dto.DueDate = summary.DueDate;
        dto.PaymentDate = summary.PaymentDate;
        dto.PaidAt = summary.PaidAt;
        dto.Status = summary.Status;
        dto.IsLate = summary.IsLate;
        dto.CanPay = summary.CanPay;
        dto.StudentName = summary.StudentName;
        dto.StudentEmail = summary.StudentEmail;
        dto.ApartmentAddress = summary.ApartmentAddress;
        dto.LandlordName = summary.LandlordName;
        dto.ApartmentCity = payment.Apartment.City;
        dto.ContractStartDate = payment.Contract.StartDate;
        dto.ContractEndDate = payment.Contract.EndDate;
        dto.MonthlyRate = payment.Contract.Booking.PricePerMonthAtBooking > 0 ? payment.Contract.Booking.PricePerMonthAtBooking : payment.Apartment.PricePerMonth;
        dto.BillingMonths = CalculateMonths(payment.Contract.StartDate, payment.Contract.EndDate);
        dto.StripeSessionId = payment.StripeSessionId;
        dto.StripePaymentIntentId = payment.StripePaymentIntentId;
        return dto;
    }

    private async Task NotifyPaymentSuccessAsync(Payment payment)
    {
        if (!string.IsNullOrWhiteSpace(payment.Student.ApplicationUser.Email))
            await _emailService.SendEmailAsync(payment.Student.ApplicationUser.Email!, "Payment Confirmed - Contract Activated!", $"<p>Your payment of {payment.Amount:C} for {payment.Apartment.Address} was successful. Your contract is now active.</p>");
        await _notificationService.CreateNotificationAsync(
            payment.Student.ApplicationUserId,
            "Payment Successful",
            $"Your payment for {payment.Apartment.Address} was successful.",
            NotificationType.PaymentSuccessful,
            $"/Student/PaymentDetails/{payment.PaymentId}");
        if (!string.IsNullOrWhiteSpace(payment.Landlord.ApplicationUser.Email))
            await _emailService.SendEmailAsync(payment.Landlord.ApplicationUser.Email!, $"Payment Received for {payment.Apartment.Address}", $"<p>{payment.Student.ApplicationUser.Name} paid {payment.Amount:C} for the contract period.</p>");
        await _notificationService.CreateNotificationAsync(
            payment.Landlord.ApplicationUserId,
            "Payment Received",
            $"{payment.Student.ApplicationUser.Name} paid for {payment.Apartment.Address}.",
            NotificationType.PaymentSuccessful,
            $"/Landlord/PaymentDetails/{payment.PaymentId}");
    }
}
