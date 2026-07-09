using Microsoft.EntityFrameworkCore;
using Sakanak.DAL.Data;
using Sakanak.DAL.Repositories.Interfaces;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.DAL.Repositories.Implementations;

public class ApartmentGroupRepository : RepositoryBase<ApartmentGroup>, IApartmentGroupRepository
{
    public ApartmentGroupRepository(SakanakDbContext context) : base(context)
    {
    }

    public async Task<ApartmentGroup?> GetOpenGroupByApartmentIdAsync(int apartmentId)
        => await DbSet.FirstOrDefaultAsync(e => e.ApartmentId == apartmentId && e.GroupStatus == GroupStatus.Open);

    public async Task<IEnumerable<ApartmentGroup>> GetFullGroupsAsync()
        => await DbSet
            .Include(e => e.Students)
            .Where(e => e.GroupStatus == GroupStatus.Full || e.Students.Count >= e.MaxMembers)
            .ToListAsync();

    public async Task<ApartmentGroup?> GetGroupWithStudentsAsync(int groupId)
        => await DbSet.Include(e => e.Students).FirstOrDefaultAsync(e => e.GroupId == groupId);

    public async Task<int> GetCurrentMemberCountAsync(int groupId)
        => await Context.Students.CountAsync(e => e.ApartmentGroupId == groupId);

    public async Task<bool> HasOpenGroupAsync(int apartmentId)
        => await DbSet.AnyAsync(e => e.ApartmentId == apartmentId && e.GroupStatus == GroupStatus.Open);
}
