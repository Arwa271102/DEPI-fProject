using Microsoft.EntityFrameworkCore;
using Sakanak.DAL.Data;
using Sakanak.DAL.Repositories.Interfaces;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.DAL.Repositories.Implementations;

public class RequestRepository : RepositoryBase<Request>, IRequestRepository
{
    public RequestRepository(SakanakDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Request>> GetByLandlordIdAsync(int landlordId)
        => await DbSet.Where(e => e.LandlordId == landlordId).ToListAsync();

    public async Task<IEnumerable<Request>> GetByStatusAsync(RequestStatus status)
        => await DbSet.Where(e => e.Status == status).ToListAsync();

    public async Task<IEnumerable<Request>> GetPendingApartmentRequestsAsync()
        => await DbSet.Where(e => e.Status == RequestStatus.Pending).ToListAsync();

    public async Task<Request?> GetByApartmentIdAsync(int apartmentId)
        => await DbSet
            .Where(e => e.ApartmentId == apartmentId)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task DeleteByApartmentAsync(int apartmentId)
    {
        var requests = await DbSet.Where(e => e.ApartmentId == apartmentId).ToListAsync();
        if (requests.Count > 0)
        {
            DbSet.RemoveRange(requests);
        }
    }

    public IQueryable<Request> GetRequestsWithDetailsQuery()
        => DbSet
            .Include(request => request.Apartment)
                .ThenInclude(apartment => apartment.Media)
            .Include(request => request.Landlord)
                .ThenInclude(landlord => landlord.ApplicationUser)
            .Include(request => request.ReviewedByAdmin)
                .ThenInclude(admin => admin!.ApplicationUser)
            .AsQueryable();
}
