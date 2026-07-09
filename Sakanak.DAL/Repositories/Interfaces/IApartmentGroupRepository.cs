using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Repositories.Interfaces;

/// <summary>
/// Provides data access operations for apartment group records.
/// </summary>
public interface IApartmentGroupRepository
{
    Task<IEnumerable<ApartmentGroup>> GetAllAsync();
    Task<ApartmentGroup?> GetByIdAsync(int id);
    Task AddAsync(ApartmentGroup entity);
    Task UpdateAsync(ApartmentGroup entity);
    Task DeleteAsync(int id);
    Task<ApartmentGroup?> GetOpenGroupByApartmentIdAsync(int apartmentId);
    Task<IEnumerable<ApartmentGroup>> GetFullGroupsAsync();
    Task<ApartmentGroup?> GetGroupWithStudentsAsync(int groupId);
    Task<int> GetCurrentMemberCountAsync(int groupId);
    Task<bool> HasOpenGroupAsync(int apartmentId);
}
