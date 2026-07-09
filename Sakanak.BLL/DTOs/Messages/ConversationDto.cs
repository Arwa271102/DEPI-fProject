namespace Sakanak.BLL.DTOs.Messages;

public class ConversationDto
{
    public Guid OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string LastMessagePreview { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public bool IsAdminConversation { get; set; }
}
