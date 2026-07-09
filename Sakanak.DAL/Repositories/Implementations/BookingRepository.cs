using Microsoft.EntityFrameworkCore;
using Sakanak.DAL.Data;
using Sakanak.DAL.Repositories.Interfaces;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.DAL.Repositories.Implementations;

public class BookingRepository : RepositoryBase<Booking>, IBookingRepository
{
    public BookingRepository(SakanakDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Booking>> GetByStudentIdAsync(int studentId)
        => await DbSet.Where(e => e.StudentId == studentId).ToListAsync();

    public async Task<IEnumerable<Booking>> GetByApartmentIdAsync(int apartmentId)
        => await DbSet.Where(e => e.ApartmentId == apartmentId).ToListAsync();

    public async Task<IEnumerable<Booking>> GetPendingBookingsAsync()
        => await DbSet.Where(e => e.Status == BookingStatus.Pending).ToListAsync();

    public async Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatus status)
        => await DbSet.Where(e => e.Status == status).ToListAsync();

    public async Task<IEnumerable<Booking>> GetBookingsByLandlordIdAsync(int landlordId)
        => await DbSet
            .Include(e => e.Apartment)
            .Where(e => e.Apartment.LandlordId == landlordId)
            .ToListAsync();
}
