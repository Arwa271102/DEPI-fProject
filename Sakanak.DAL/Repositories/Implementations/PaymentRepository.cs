using Microsoft.EntityFrameworkCore;
using Sakanak.DAL.Data;
using Sakanak.DAL.Repositories.Interfaces;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.DAL.Repositories.Implementations;

public class PaymentRepository : RepositoryBase<Payment>, IPaymentRepository
{
    public PaymentRepository(SakanakDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Payment>> GetByStudentIdAsync(int studentId)
        => await DbSet.Where(e => e.StudentId == studentId).ToListAsync();

    public async Task<IEnumerable<Payment>> GetByLandlordIdAsync(int landlordId)
        => await DbSet.Where(e => e.LandlordId == landlordId).ToListAsync();

    public async Task<IEnumerable<Payment>> GetByContractIdAsync(int contractId)
        => await DbSet.Where(e => e.ContractId == contractId).ToListAsync();

    public async Task<IEnumerable<Payment>> GetPendingPaymentsAsync()
        => await DbSet.Where(e => e.Status == PaymentStatus.Pending).ToListAsync();

    public async Task<IEnumerable<Payment>> GetOverduePaymentsAsync()
        => await DbSet
            .Where(e => e.Status == PaymentStatus.Pending && e.DueDate < DateTime.UtcNow)
            .ToListAsync();

    public async Task<IEnumerable<Payment>> GetByStatusAsync(PaymentStatus status)
        => await DbSet.Where(e => e.Status == status).ToListAsync();

    public async Task<bool> HasPaidPaymentForContractAsync(int contractId, int studentId)
        => await DbSet.AnyAsync(e => e.ContractId == contractId && e.StudentId == studentId && e.Status == PaymentStatus.Paid);
}
