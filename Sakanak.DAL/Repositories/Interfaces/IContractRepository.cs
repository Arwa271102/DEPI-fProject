using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.DAL.Repositories.Interfaces;

/// <summary>
/// Provides data access operations for contract records.
/// </summary>
public interface IContractRepository
{
    Task<IEnumerable<Contract>> GetAllAsync();
    Task<Contract?> GetByIdAsync(int id);
    Task AddAsync(Contract entity);
    Task UpdateAsync(Contract entity);
    Task DeleteAsync(int id);
    Task<IEnumerable<Contract>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<Contract>> GetByApartmentIdAsync(int apartmentId);
    Task<IEnumerable<Contract>> GetByStatusAsync(ContractStatus status);
    Task<IEnumerable<Contract>> GetPendingApprovalContractsAsync();
    Task<IEnumerable<Contract>> GetApprovedContractsAsync();
    Task<IEnumerable<Contract>> GetActiveContractsAsync();
    Task<Contract?> GetContractWithPaymentsAsync(int contractId);
    Task<Contract?> GetByBookingIdAsync(int bookingId);
    Task<IEnumerable<Contract>> GetExpiringContractsAsync(DateTime beforeDate);
}
