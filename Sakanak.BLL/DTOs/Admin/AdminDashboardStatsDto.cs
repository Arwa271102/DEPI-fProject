namespace Sakanak.BLL.DTOs.Admin;

public class AdminDashboardStatsDto
{
    public int PendingRequestsCount { get; set; }
    public int TotalReviewedRequests { get; set; }
    public int ApprovedRequestsCount { get; set; }
    public int RejectedRequestsCount { get; set; }
    public int TotalLandlords { get; set; }
    public int SuspendedLandlords { get; set; }
    public int TotalApartments { get; set; }
    public int PendingLandlordVerificationsCount { get; set; }
}
