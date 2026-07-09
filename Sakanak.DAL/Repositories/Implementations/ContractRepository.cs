using Microsoft.EntityFrameworkCore;
using Sakanak.DAL.Data;
using Sakanak.DAL.Repositories.Interfaces;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.DAL.Repositories.Implementations;

public class ContractRepository : RepositoryBase<Contract>, IContractRepository
{
    public ContractRepository(SakanakDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Contract>> GetByStudentIdAsync(int studentId)
        => await DbSet.Where(e => e.StudentId == studentId).ToListAsync();

    public async Task<IEnumerable<Contract>> GetByApartmentIdAsync(int apartmentId)
        => await DbSet.Where(e => e.ApartmentId == apartmentId).ToListAsync();

    public async Task<IEnumerable<Contract>> GetByStatusAsync(ContractStatus status)
        => await DbSet.Where(e => e.Status == status).ToListAsync();

    public async Task<IEnumerable<Contract>> GetPendingApprovalContractsAsync()
        => await DbSet.Where(e => e.Status == ContractStatus.PendingApproval).ToListAsync();

    public async Task<IEnumerable<Contract>> GetApprovedContractsAsync()
        => await DbSet.Where(e => e.Status == ContractStatus.Approved).ToListAsync();

    public async Task<IEnumerable<Contract>> GetActiveContractsAsync()
        => await DbSet.Where(e => e.Status == ContractStatus.Active).ToListAsync();

    public async Task<Contract?> GetContractWithPaymentsAsync(int contractId)
        => await DbSet
            .Include(e => e.Payments)
            .FirstOrDefaultAsync(e => e.ContractId == contractId);

    public async Task<Contract?> GetByBookingIdAsync(int bookingId)
        => await DbSet.FirstOrDefaultAsync(e => e.BookingId == bookingId);

    public async Task<IEnumerable<Contract>> GetExpiringContractsAsync(DateTime beforeDate)
        => await DbSet
            .Where(e => e.Status == ContractStatus.Active && e.EndDate <= beforeDate)
            .ToListAsync();
}
