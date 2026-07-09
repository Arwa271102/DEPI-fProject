using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.DAL.Repositories.Interfaces;

/// <summary>
/// Provides data access operations for booking records.
/// </summary>
public interface IBookingRepository
{
    Task<IEnumerable<Booking>> GetAllAsync();
    Task<Booking?> GetByIdAsync(int id);
    Task AddAsync(Booking entity);
    Task UpdateAsync(Booking entity);
    Task DeleteAsync(int id);
    Task<IEnumerable<Booking>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<Booking>> GetByApartmentIdAsync(int apartmentId);
    Task<IEnumerable<Booking>> GetPendingBookingsAsync();
    Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatus status);
    Task<IEnumerable<Booking>> GetBookingsByLandlordIdAsync(int landlordId);
}
