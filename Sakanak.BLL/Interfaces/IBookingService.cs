using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Booking;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Interfaces;

public interface IBookingService
{
    Task<Result<int>> CreateBookingAsync(int studentId, CreateBookingDto dto);
    Task<Result<List<BookingDto>>> GetStudentBookingsAsync(int studentId, BookingStatus? statusFilter = null);
    Task<Result<BookingDetailsDto>> GetBookingDetailsAsync(int bookingId, int studentId);
    Task<Result> CancelBookingAsync(int bookingId, int studentId);
    Task<Result<List<BookingDto>>> GetLandlordBookingsAsync(int landlordId, int? apartmentId = null, BookingStatus? statusFilter = null);
    Task<Result<BookingDetailsDto>> GetBookingDetailsForLandlordAsync(int bookingId, int landlordId);
    Task<Result> AcceptBookingAsync(int bookingId, int landlordId);
    Task<Result> RejectBookingAsync(int bookingId, int landlordId, string reason);
    Task<Result<int>> GetAvailableSeatsAsync(int apartmentId, DateTime startDate, DateTime endDate);
    Task<Result<int>> GetAvailableSeatsAsync(int apartmentId, DateTime? forDate = null);
    Task<Result<bool>> CanStudentBookAsync(int studentId);
}
