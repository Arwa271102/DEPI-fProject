using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Repositories.Interfaces;

/// <summary>
/// Provides data access operations for apartment records.
/// </summary>
public interface IApartmentRepository
{
    Task<IEnumerable<Apartment>> GetAllAsync();
    Task<Apartment?> GetByIdAsync(int id);
    Task AddAsync(Apartment entity);
    Task UpdateAsync(Apartment entity);
    Task DeleteAsync(int id);
    Task<IEnumerable<Apartment>> GetActiveApartmentsAsync();
    Task<IEnumerable<Apartment>> GetByLandlordIdAsync(int landlordId);
    Task<IEnumerable<Apartment>> GetByCityAsync(string city);
    Task<IEnumerable<Apartment>> GetApartmentsWithAvailableSeatsAsync();
    Task<Apartment?> GetApartmentWithGroupsAsync(int apartmentId);
    Task<IEnumerable<Apartment>> GetApartmentsByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    IQueryable<Apartment> GetApartmentsWithDetailsQuery();
    Task<Apartment?> GetApartmentForDeletionAsync(int apartmentId);
}
