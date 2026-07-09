using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Repositories.Interfaces;

/// <summary>
/// Provides data access operations for media records.
/// </summary>
public interface IMediaRepository
{
    Task<IEnumerable<Media>> GetAllAsync();
    Task<Media?> GetByIdAsync(int id);
    Task AddAsync(Media entity);
    Task UpdateAsync(Media entity);
    Task DeleteAsync(int id);
    Task<IEnumerable<Media>> GetByEntityAsync(string entityType, int entityId);
    Task<IEnumerable<Media>> GetImagesByEntityAsync(string entityType, int entityId);
    Task<IEnumerable<Media>> GetDocumentsByEntityAsync(string entityType, int entityId);
    Task DeleteByEntityAsync(string entityType, int entityId);
}
