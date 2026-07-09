namespace Sakanak.Domain.Entities;

public class Message
{
    public int MessageId { get; set; }
    public Guid SenderUserId { get; set; }
    public Guid RecipientUserId { get; set; }
    public string SenderType { get; set; } = string.Empty;
    public string RecipientType { get; set; } = string.Empty;
    public int? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public ApplicationUser SenderUser { get; set; } = null!;
    public ApplicationUser RecipientUser { get; set; } = null!;
}
