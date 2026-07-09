using Sakanak.BLL.DTOs.Booking;
using Sakanak.Domain.Enums;

namespace Sakanak.Web.Models.ViewModels.Landlord;

public class BookingRequestsViewModel
{
    public IReadOnlyList<BookingDto> Bookings { get; set; } = Array.Empty<BookingDto>();
    public IReadOnlyList<ApartmentOptionViewModel> Apartments { get; set; } = Array.Empty<ApartmentOptionViewModel>();
    public int? ApartmentId { get; set; }
    public BookingStatus? StatusFilter { get; set; }
    public int PendingCount { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
}
