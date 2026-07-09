using Microsoft.EntityFrameworkCore;
using Sakanak.DAL.Data;
using Sakanak.DAL.Repositories.Interfaces;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.DAL.Repositories.Implementations;

public class MediaRepository : RepositoryBase<Media>, IMediaRepository
{
    public MediaRepository(SakanakDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Media>> GetByEntityAsync(string entityType, int entityId)
        => await DbSet.Where(e => e.EntityType == entityType && e.EntityId == entityId).ToListAsync();

    public async Task<IEnumerable<Media>> GetImagesByEntityAsync(string entityType, int entityId)
        => await DbSet
            .Where(e => e.EntityType == entityType && e.EntityId == entityId && e.Type == MediaType.Image)
            .ToListAsync();

    public async Task<IEnumerable<Media>> GetDocumentsByEntityAsync(string entityType, int entityId)
        => await DbSet
            .Where(e => e.EntityType == entityType && e.EntityId == entityId && e.Type == MediaType.Document)
            .ToListAsync();

    public async Task DeleteByEntityAsync(string entityType, int entityId)
    {
        var entities = await DbSet.Where(e => e.EntityType == entityType && e.EntityId == entityId).ToListAsync();
        if (entities.Count > 0)
        {
            DbSet.RemoveRange(entities);
        }
    }
}
