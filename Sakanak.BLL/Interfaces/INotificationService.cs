using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Common;
using Sakanak.BLL.DTOs.Notifications;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Interfaces;

public interface INotificationService
{
    Task<Result<int>> CreateNotificationAsync(Guid userId, string title, string message, NotificationType type, string? actionUrl = null);
    Task<Result<PagedResult<NotificationDto>>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false, int page = 1, int pageSize = 20);
    Task<Result<List<NotificationDto>>> GetRecentNotificationsAsync(Guid userId, int count = 5);
    Task<Result> MarkAsReadAsync(int notificationId, Guid userId);
    Task<Result> MarkAsUnreadAsync(int notificationId, Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
}
