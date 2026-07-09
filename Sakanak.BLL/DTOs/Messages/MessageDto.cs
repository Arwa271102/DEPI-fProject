namespace Sakanak.BLL.DTOs.Messages;

public class MessageDto
{
    public int MessageId { get; set; }
    public Guid SenderUserId { get; set; }
    public Guid RecipientUserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsMine { get; set; }
}
