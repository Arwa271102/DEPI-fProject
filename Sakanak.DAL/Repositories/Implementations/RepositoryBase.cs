using Microsoft.EntityFrameworkCore;
using Sakanak.DAL.Data;

namespace Sakanak.DAL.Repositories.Implementations;

public abstract class RepositoryBase<TEntity> where TEntity : class
{
    protected RepositoryBase(SakanakDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    protected SakanakDbContext Context { get; }
    protected DbSet<TEntity> DbSet { get; }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public virtual async Task<TEntity?> GetByIdAsync(int id)
    {
        return await DbSet.FindAsync(id);
    }

    public virtual async Task AddAsync(TEntity entity)
    {
        await DbSet.AddAsync(entity);
    }

    public virtual Task UpdateAsync(TEntity entity)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity is not null)
        {
            DbSet.Remove(entity);
        }
    }
}
