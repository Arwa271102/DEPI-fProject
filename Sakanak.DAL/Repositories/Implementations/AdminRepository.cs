using Microsoft.EntityFrameworkCore;
using Sakanak.DAL.Data;
using Sakanak.DAL.Repositories.Interfaces;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.DAL.Repositories.Implementations;

public class AdminRepository : RepositoryBase<Admin>, IAdminRepository
{
    public AdminRepository(SakanakDbContext context) : base(context)
    {
    }

    public async Task<Admin?> GetByEmailAsync(string email)
        => await DbSet
            .Include(e => e.ApplicationUser)
            .FirstOrDefaultAsync(e => e.ApplicationUser.Email == email);

    public async Task<IEnumerable<Admin>> GetSuperAdminsAsync()
        => await DbSet.Where(e => e.RoleLevel == AdminRoleLevel.SuperAdmin).ToListAsync();

    public async Task<Admin?> GetByUserIdAsync(Guid userId)
        => await DbSet.AsNoTracking().FirstOrDefaultAsync(e => e.ApplicationUserId == userId);

    public IQueryable<Admin> GetAdminsWithUsersQuery()
        => DbSet
            .Include(admin => admin.ApplicationUser)
            .AsQueryable();
}
