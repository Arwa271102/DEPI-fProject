using Microsoft.EntityFrameworkCore;
using Sakanak.DAL.Data;
using Sakanak.DAL.Repositories.Interfaces;
using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Repositories.Implementations;

public class LandlordRepository : RepositoryBase<Landlord>, ILandlordRepository
{
    public LandlordRepository(SakanakDbContext context) : base(context)
    {
    }

    public async Task<Landlord?> GetByEmailAsync(string email)
        => await DbSet
            .Include(e => e.ApplicationUser)
            .FirstOrDefaultAsync(e => e.ApplicationUser.Email == email);

    public async Task<IEnumerable<Landlord>> GetVerifiedLandlordsAsync()
        => await DbSet.Where(e => e.VerificationStatus).ToListAsync();

    public async Task<Landlord?> GetLandlordWithApartmentsAsync(int landlordId)
        => await DbSet.Include(e => e.Apartments).FirstOrDefaultAsync(e => e.LandlordId == landlordId);

    public async Task<int> GetTotalPropertiesCountAsync(int landlordId)
        => await Context.Apartments.CountAsync(e => e.LandlordId == landlordId);

    public async Task<Landlord?> GetByUserIdAsync(Guid userId)
        => await DbSet.AsNoTracking().FirstOrDefaultAsync(e => e.ApplicationUserId == userId);

    public IQueryable<Landlord> GetLandlordsWithDetailsQuery()
        => DbSet
            .Include(landlord => landlord.ApplicationUser)
            .Include(landlord => landlord.Apartments)
            .Include(landlord => landlord.Media)
            .AsQueryable();
}
