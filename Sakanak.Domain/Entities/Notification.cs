using Sakanak.Domain.Enums;

namespace Sakanak.Domain.Entities;

public class Notification
{
    public int NotificationId { get; set; }
    public Guid RecipientUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ApplicationUser RecipientUser { get; set; } = null!;
}
