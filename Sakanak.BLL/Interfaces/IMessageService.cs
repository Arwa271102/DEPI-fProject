using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Messages;

namespace Sakanak.BLL.Interfaces;

public interface IMessageService
{
    Task<Result<int>> SendMessageAsync(Guid senderUserId, Guid recipientUserId, string messageText, string senderType, string recipientType, int? relatedEntityId = null, string? relatedEntityType = null);
    Task<Result<List<MessageDto>>> GetConversationAsync(Guid userId, Guid otherUserId);
    Task<Result<List<ConversationDto>>> GetUserConversationsAsync(Guid userId);
    Task<int> GetUnreadMessageCountAsync(Guid userId);
    Task<Result> MarkConversationAsReadAsync(Guid userId, Guid otherUserId);
}
