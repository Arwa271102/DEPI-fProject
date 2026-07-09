using Sakanak.BLL.DTOs.Search;
using Sakanak.BLL.DTOs.Student;

namespace Sakanak.Web.Models.ViewModels.Student;

public class StudentDashboardViewModel
{
    public StudentProfileDto Profile { get; set; } = new();
    public IReadOnlyList<ApartmentListItemDto> FeaturedApartments { get; set; } = Array.Empty<ApartmentListItemDto>();
    public IReadOnlyList<string> Cities { get; set; } = Array.Empty<string>();
    public int ActiveBookingsCount { get; set; }
    public IReadOnlyList<Sakanak.BLL.DTOs.Booking.BookingDto> RecentBookings { get; set; } = Array.Empty<Sakanak.BLL.DTOs.Booking.BookingDto>();
    public IReadOnlyList<Sakanak.BLL.DTOs.Contract.ContractDto> RecentContracts { get; set; } = Array.Empty<Sakanak.BLL.DTOs.Contract.ContractDto>();
    public IReadOnlyList<Sakanak.BLL.DTOs.Payment.PaymentDto> RecentPayments { get; set; } = Array.Empty<Sakanak.BLL.DTOs.Payment.PaymentDto>();
    public int PendingContracts { get; set; }
    public int ApprovedContracts { get; set; }
    public int RejectedContracts { get; set; }
    public int PendingPayments { get; set; }
    public int PaidPayments { get; set; }
    public Sakanak.BLL.DTOs.Apartment.StudentApartmentAssignmentDto? CurrentApartment { get; set; }

    public List<Sakanak.BLL.DTOs.Notifications.NotificationDto> RecentNotifications { get; set; } = new();
    public List<Sakanak.BLL.DTOs.Messages.ConversationDto> RecentMessages { get; set; } = new();
    public Guid? AdminUserId { get; set; }
}
