using Sakanak.BLL.DTOs.Booking;
using Sakanak.Domain.Enums;

namespace Sakanak.Web.Models.ViewModels.Student;

public class MyBookingsViewModel
{
    public IReadOnlyList<BookingDto> Bookings { get; set; } = Array.Empty<BookingDto>();
    public BookingStatus? StatusFilter { get; set; }
}
