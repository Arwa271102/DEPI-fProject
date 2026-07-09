using Sakanak.Domain.Entities;
namespace Sakanak.DAL.Repositories.Interfaces;

/// <summary>
/// Provides data access operations for request records.
/// </summary>
public interface IRequestRepository
{
    Task<IEnumerable<Request>> GetAllAsync();
    Task<Request?> GetByIdAsync(int id);
    Task AddAsync(Request entity);
    Task UpdateAsync(Request entity);
    Task DeleteAsync(int id);
    Task<IEnumerable<Request>> GetByLandlordIdAsync(int landlordId);
    Task<IEnumerable<Request>> GetByStatusAsync(Sakanak.Domain.Enums.RequestStatus status);
    Task<IEnumerable<Request>> GetPendingApartmentRequestsAsync();
    Task<Request?> GetByApartmentIdAsync(int apartmentId);
    Task DeleteByApartmentAsync(int apartmentId);
    IQueryable<Request> GetRequestsWithDetailsQuery();
}
