using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.DAL.Repositories.Interfaces;

/// <summary>
/// Provides data access operations for payment records.
/// </summary>
public interface IPaymentRepository
{
    Task<IEnumerable<Payment>> GetAllAsync();
    Task<Payment?> GetByIdAsync(int id);
    Task AddAsync(Payment entity);
    Task UpdateAsync(Payment entity);
    Task DeleteAsync(int id);
    Task<IEnumerable<Payment>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<Payment>> GetByLandlordIdAsync(int landlordId);
    Task<IEnumerable<Payment>> GetByContractIdAsync(int contractId);
    Task<IEnumerable<Payment>> GetPendingPaymentsAsync();
    Task<IEnumerable<Payment>> GetOverduePaymentsAsync();
    Task<IEnumerable<Payment>> GetByStatusAsync(PaymentStatus status);
    Task<bool> HasPaidPaymentForContractAsync(int contractId, int studentId);
}
