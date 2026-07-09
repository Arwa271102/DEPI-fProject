namespace Sakanak.Web.Models.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int PendingRequestsCount { get; set; }
    public int TotalReviewedRequests { get; set; }
    public int ApprovedRequestsCount { get; set; }
    public int RejectedRequestsCount { get; set; }
    public int TotalLandlords { get; set; }
    public int SuspendedLandlords { get; set; }
    public int TotalApartments { get; set; }
    public int PendingLandlordVerificationsCount { get; set; }
    public int PendingContractsCount { get; set; }
    public decimal TotalPaidPaymentsAmount { get; set; }
    public decimal PendingPaymentsAmount { get; set; }
    public decimal PaymentsThisMonthAmount { get; set; }
    public int PendingPaymentsCount { get; set; }
    public int PlatformTotalSeats { get; set; }
    public int PlatformOccupiedSeats { get; set; }
    public decimal PlatformOccupancyRate { get; set; }
    public int ActiveContractsCount { get; set; }
    public DateTime? OldestPendingVerificationRequestedAt { get; set; }
    public IReadOnlyList<PendingRequestListItemViewModel> RecentPendingRequests { get; set; } = Array.Empty<PendingRequestListItemViewModel>();
    public IReadOnlyList<Sakanak.BLL.DTOs.Admin.LandlordVerificationRequestDto> RecentPendingLandlords { get; set; } = Array.Empty<Sakanak.BLL.DTOs.Admin.LandlordVerificationRequestDto>();
    public IReadOnlyList<Sakanak.BLL.DTOs.Contract.ContractDto> RecentPendingContracts { get; set; } = Array.Empty<Sakanak.BLL.DTOs.Contract.ContractDto>();
    public IReadOnlyList<Sakanak.BLL.DTOs.Contract.ContractDto> RecentReviewedContracts { get; set; } = Array.Empty<Sakanak.BLL.DTOs.Contract.ContractDto>();
    public IReadOnlyList<Sakanak.BLL.DTOs.Payment.PaymentDto> RecentPayments { get; set; } = Array.Empty<Sakanak.BLL.DTOs.Payment.PaymentDto>();

    public List<Sakanak.BLL.DTOs.Notifications.NotificationDto> RecentNotifications { get; set; } = new();
    public List<Sakanak.BLL.DTOs.Messages.ConversationDto> RecentMessages { get; set; } = new();
}
