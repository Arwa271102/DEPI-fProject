using Sakanak.Web.Models.ViewModels.Shared;

namespace Sakanak.Web.Models.ViewModels.Landlord;

public class LandlordDashboardViewModel
{
    public int TotalApartments { get; set; }
    public int ActiveApartments { get; set; }
    public int PendingRequests { get; set; }
    public int RejectedRequests { get; set; }
    public int PendingBookingRequests { get; set; }
    public int OccupiedApartmentsCount { get; set; }
    public IReadOnlyList<ApartmentListItemViewModel> Apartments { get; set; } = Array.Empty<ApartmentListItemViewModel>();
    public IReadOnlyList<RequestListItemViewModel> RecentRequests { get; set; } = Array.Empty<RequestListItemViewModel>();
    public IReadOnlyList<Sakanak.BLL.DTOs.Booking.BookingDto> RecentBookingRequests { get; set; } = Array.Empty<Sakanak.BLL.DTOs.Booking.BookingDto>();
    public IReadOnlyList<Sakanak.BLL.DTOs.Apartment.ApartmentOccupancyDto> RecentOccupiedApartments { get; set; } = Array.Empty<Sakanak.BLL.DTOs.Apartment.ApartmentOccupancyDto>();
    public IReadOnlyList<Sakanak.BLL.DTOs.Payment.PaymentDto> RecentPayments { get; set; } = Array.Empty<Sakanak.BLL.DTOs.Payment.PaymentDto>();
    public IReadOnlyList<Sakanak.BLL.DTOs.Contract.ContractDto> RecentContracts { get; set; } = Array.Empty<Sakanak.BLL.DTOs.Contract.ContractDto>();
    public int PendingContracts { get; set; }
    public int ApprovedContracts { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal PendingPaymentAmount { get; set; }
    public int OccupiedSeats { get; set; }
    public int TotalSeats { get; set; }
    public decimal OccupancyRate { get; set; }
    public PaginationViewModel ApartmentsPagination { get; set; } = new();
    public string? CityFilter { get; set; }
    public string? RequestStatusFilter { get; set; }
    public string SortBy { get; set; } = "created";
    public bool Descending { get; set; } = true;

    public List<Sakanak.BLL.DTOs.Notifications.NotificationDto> RecentNotifications { get; set; } = new();
    public List<Sakanak.BLL.DTOs.Messages.ConversationDto> RecentMessages { get; set; } = new();
    public Guid? AdminUserId { get; set; }
}
