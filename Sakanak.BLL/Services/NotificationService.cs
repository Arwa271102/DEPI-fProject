using Microsoft.EntityFrameworkCore;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Common;
using Sakanak.BLL.DTOs.Notifications;
using Sakanak.BLL.Interfaces;
using Sakanak.DAL.Data;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Services;

public class NotificationService : INotificationService
{
    private readonly SakanakDbContext _dbContext;

    public NotificationService(SakanakDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<int>> CreateNotificationAsync(Guid userId, string title, string message, NotificationType type, string? actionUrl = null)
    {
        var notification = new Notification
        {
            RecipientUserId = userId,
            Title = title,
            Message = message,
            Type = type,
            ActionUrl = actionUrl,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();
        return Result<int>.Success(notification.NotificationId);
    }

    public async Task<Result<PagedResult<NotificationDto>>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false, int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = _dbContext.Notifications.Where(item => item.RecipientUserId == userId);
        if (unreadOnly)
        {
            query = query.Where(item => !item.IsRead);
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(item => item.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Result<PagedResult<NotificationDto>>.Success(new PagedResult<NotificationDto>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<List<NotificationDto>>> GetRecentNotificationsAsync(Guid userId, int count = 5)
        => Result<List<NotificationDto>>.Success(await _dbContext.Notifications
            .Where(item => item.RecipientUserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(count)
            .Select(item => Map(item))
            .ToListAsync());

    public async Task<Result> MarkAsReadAsync(int notificationId, Guid userId)
        => await SetReadStateAsync(notificationId, userId, true);

    public async Task<Result> MarkAsUnreadAsync(int notificationId, Guid userId)
        => await SetReadStateAsync(notificationId, userId, false);

    public async Task<int> GetUnreadCountAsync(Guid userId)
        => await _dbContext.Notifications.CountAsync(item => item.RecipientUserId == userId && !item.IsRead);

    private async Task<Result> SetReadStateAsync(int notificationId, Guid userId, bool isRead)
    {
        var notification = await _dbContext.Notifications.FirstOrDefaultAsync(item => item.NotificationId == notificationId && item.RecipientUserId == userId);
        if (notification is null)
        {
            return Result.Failure("Notification was not found.");
        }

        notification.IsRead = isRead;
        await _dbContext.SaveChangesAsync();
        return Result.Success();
    }

    private static NotificationDto Map(Notification notification)
        => new()
        {
            NotificationId = notification.NotificationId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type.ToString(),
            ActionUrl = notification.ActionUrl,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
}
