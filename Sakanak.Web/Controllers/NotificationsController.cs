using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sakanak.BLL.DTOs.Common;
using Sakanak.BLL.Interfaces;

namespace Sakanak.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, bool unreadOnly = false)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Challenge();

        var result = await _notificationService.GetUserNotificationsAsync(userId.Value, unreadOnly, page, 20);
        return View(result.Succeeded ? result.Data : new PagedResult<Sakanak.BLL.DTOs.Notifications.NotificationDto>());
    }

    [HttpGet]
    public async Task<IActionResult> Open(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Challenge();

        var notifications = await _notificationService.GetUserNotificationsAsync(userId.Value, pageSize: 1);
        await _notificationService.MarkAsReadAsync(id, userId.Value);
        var result = await _notificationService.GetUserNotificationsAsync(userId.Value, pageSize: 100);
        var notification = result.Data?.Items.FirstOrDefault(item => item.NotificationId == id);
        if (!string.IsNullOrWhiteSpace(notification?.ActionUrl))
        {
            return LocalRedirect(notification.ActionUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsUnread(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Challenge();

        await _notificationService.MarkAsUnreadAsync(id, userId.Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Challenge();

        // Fetch all unread and mark each as read
        var result = await _notificationService.GetUserNotificationsAsync(userId.Value, unreadOnly: true, pageSize: 200);
        if (result.Succeeded && result.Data != null)
        {
            foreach (var notif in result.Data.Items)
            {
                await _notificationService.MarkAsReadAsync(notif.NotificationId, userId.Value);
            }
        }
        TempData["SuccessMessage"] = "All notifications marked as read.";
        return RedirectToAction(nameof(Index));
    }

    private Guid? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }
}
