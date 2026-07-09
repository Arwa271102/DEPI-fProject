using Microsoft.EntityFrameworkCore;
using Sakanak.DAL.Data;
using Sakanak.DAL.Repositories.Interfaces;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.DAL.Repositories.Implementations;

public class ApartmentRepository : RepositoryBase<Apartment>, IApartmentRepository
{
    public ApartmentRepository(SakanakDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Apartment>> GetActiveApartmentsAsync()
        => await DbSet.Where(e => e.IsActive).ToListAsync();

    public async Task<IEnumerable<Apartment>> GetByLandlordIdAsync(int landlordId)
        => await DbSet.Where(e => e.LandlordId == landlordId).ToListAsync();

    public async Task<IEnumerable<Apartment>> GetByCityAsync(string city)
        => await DbSet.Where(e => e.City == city).ToListAsync();

    public async Task<IEnumerable<Apartment>> GetApartmentsWithAvailableSeatsAsync()
        => await DbSet
            .Include(e => e.ApartmentGroups)
            .ThenInclude(e => e.Students)
            .Where(e => e.IsActive && (!e.ApartmentGroups.Any()
                || e.ApartmentGroups.Any(g => g.GroupStatus == GroupStatus.Open && g.Students.Count < g.MaxMembers)))
            .ToListAsync();

    public async Task<Apartment?> GetApartmentWithGroupsAsync(int apartmentId)
        => await DbSet
            .Include(e => e.ApartmentGroups)
            .ThenInclude(e => e.Students)
            .FirstOrDefaultAsync(e => e.ApartmentId == apartmentId);

    public async Task<IEnumerable<Apartment>> GetApartmentsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        => await DbSet.Where(e => e.PricePerMonth >= minPrice && e.PricePerMonth <= maxPrice).ToListAsync();

    public IQueryable<Apartment> GetApartmentsWithDetailsQuery()
        => DbSet
            .Include(apartment => apartment.Landlord)
                .ThenInclude(landlord => landlord.ApplicationUser)
            .Include(apartment => apartment.Requests)
            .Include(apartment => apartment.Media)
            .Include(apartment => apartment.Bookings)
            .AsQueryable();

    public async Task<Apartment?> GetApartmentForDeletionAsync(int apartmentId)
        => await DbSet
            .Include(item => item.Bookings)
            .Include(item => item.Contracts)
            .FirstOrDefaultAsync(item => item.ApartmentId == apartmentId);
}
