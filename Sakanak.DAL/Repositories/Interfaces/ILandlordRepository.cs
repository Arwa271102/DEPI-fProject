using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Repositories.Interfaces;

/// <summary>
/// Provides data access operations for landlord records.
/// </summary>
public interface ILandlordRepository
{
    Task<IEnumerable<Landlord>> GetAllAsync();
    Task<Landlord?> GetByIdAsync(int id);
    Task AddAsync(Landlord entity);
    Task UpdateAsync(Landlord entity);
    Task DeleteAsync(int id);
    Task<Landlord?> GetByEmailAsync(string email);
    Task<IEnumerable<Landlord>> GetVerifiedLandlordsAsync();
    Task<Landlord?> GetLandlordWithApartmentsAsync(int landlordId);
    Task<int> GetTotalPropertiesCountAsync(int landlordId);
    Task<Landlord?> GetByUserIdAsync(Guid userId);
    IQueryable<Landlord> GetLandlordsWithDetailsQuery();
}
