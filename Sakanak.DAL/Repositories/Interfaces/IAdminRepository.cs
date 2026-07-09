using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Repositories.Interfaces;

/// <summary>
/// Provides data access operations for admin records.
/// </summary>
public interface IAdminRepository
{
    Task<IEnumerable<Admin>> GetAllAsync();
    Task<Admin?> GetByIdAsync(int id);
    Task AddAsync(Admin entity);
    Task UpdateAsync(Admin entity);
    Task DeleteAsync(int id);
    Task<Admin?> GetByEmailAsync(string email);
    Task<Admin?> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Admin>> GetSuperAdminsAsync();
    IQueryable<Admin> GetAdminsWithUsersQuery();
}
