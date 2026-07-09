using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sakanak.BLL.Options;
using Sakanak.DAL.Data;
using Sakanak.Domain.Enums;

namespace Sakanak.Web.Services;

public class BookingExpiryHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingExpiryHostedService> _logger;
    private readonly BusinessRuleOptions _businessRules;

    public BookingExpiryHostedService(IServiceScopeFactory scopeFactory, ILogger<BookingExpiryHostedService> logger, IOptions<BusinessRuleOptions> businessRules)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _businessRules = businessRules.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateExpiredBookingsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update expired bookings.");
            }

            await Task.Delay(TimeSpan.FromHours(Math.Max(1, _businessRules.ContractExpiryCheckIntervalHours)), stoppingToken);
        }
    }

    private async Task UpdateExpiredBookingsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SakanakDbContext>();
        var today = DateTime.UtcNow.Date;

        var expiredContracts = await dbContext.Contracts
            .Include(contract => contract.Student)
            .Include(contract => contract.Booking)
            .Where(contract => contract.Status == ContractStatus.Active && contract.EndDate < today)
            .ToListAsync(cancellationToken);

        foreach (var contract in expiredContracts)
        {
            contract.Status = ContractStatus.Completed;
            contract.CompletedAt = DateTime.UtcNow;
            contract.Booking.Status = BookingStatus.Completed;
            contract.Booking.CompletedAt = DateTime.UtcNow;
            contract.Student.ApartmentGroupId = null;
        }

        var expiredAcceptedBookings = await dbContext.Bookings
            .Where(booking => booking.Status == BookingStatus.Accepted && booking.RequestedEndDate < today)
            .ToListAsync(cancellationToken);

        foreach (var booking in expiredAcceptedBookings)
        {
            booking.Status = BookingStatus.Completed;
            booking.CompletedAt = DateTime.UtcNow;
        }

        var totalExpired = expiredContracts.Count + expiredAcceptedBookings.Count;
        if (totalExpired > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Marked {Contracts} contract(s) and {Bookings} booking(s) as completed.", expiredContracts.Count, expiredAcceptedBookings.Count);
        }
    }
}
