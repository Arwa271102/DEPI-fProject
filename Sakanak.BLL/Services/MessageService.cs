using Microsoft.EntityFrameworkCore;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Messages;
using Sakanak.BLL.Interfaces;
using Sakanak.DAL.Data;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Services;

public class MessageService : IMessageService
{
    private readonly SakanakDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public MessageService(SakanakDbContext dbContext, INotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public async Task<Result<int>> SendMessageAsync(Guid senderUserId, Guid recipientUserId, string messageText, string senderType, string recipientType, int? relatedEntityId = null, string? relatedEntityType = null)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return Result<int>.Failure("Message cannot be empty.");
        }

        if (messageText.Trim().Length > 1000)
        {
            return Result<int>.Failure("Message cannot exceed 1000 characters.");
        }

        var message = new Message
        {
            SenderUserId = senderUserId,
            RecipientUserId = recipientUserId,
            SenderType = senderType,
            RecipientType = recipientType,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            MessageText = messageText.Trim(),
            SentAt = DateTime.UtcNow
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        var senderName = await _dbContext.Users.Where(item => item.Id == senderUserId).Select(item => item.Name).FirstOrDefaultAsync() ?? "Someone";
        await _notificationService.CreateNotificationAsync(recipientUserId, "New Message", $"{senderName}: {message.MessageText}", NotificationType.MessageReceived, $"/Messages/Index?otherUserId={senderUserId}");
        return Result<int>.Success(message.MessageId);
    }

    public async Task<Result<List<MessageDto>>> GetConversationAsync(Guid userId, Guid otherUserId)
    {
        var messages = await ConversationQuery(userId, otherUserId)
            .OrderBy(item => item.SentAt)
            .ToListAsync();

        return Result<List<MessageDto>>.Success(messages.Select(item => Map(item, userId)).ToList());
    }

    public async Task<Result<List<ConversationDto>>> GetUserConversationsAsync(Guid userId)
    {
        var messages = await _dbContext.Messages
            .Include(item => item.SenderUser)
            .Include(item => item.RecipientUser)
            .Where(item => item.SenderUserId == userId || item.RecipientUserId == userId)
            .OrderByDescending(item => item.SentAt)
            .ToListAsync();

        var conversations = messages
            .GroupBy(item => item.SenderUserId == userId ? item.RecipientUserId : item.SenderUserId)
            .Select(group =>
            {
                var latest = group.OrderByDescending(item => item.SentAt).First();
                var otherUser = latest.SenderUserId == userId ? latest.RecipientUser : latest.SenderUser;
                return new ConversationDto
                {
                    OtherUserId = otherUser.Id,
                    OtherUserName = otherUser.Name,
                    LastMessagePreview = latest.MessageText.Length > 80 ? latest.MessageText[..80] + "..." : latest.MessageText,
                    LastMessageAt = latest.SentAt,
                    UnreadCount = group.Count(item => item.RecipientUserId == userId && !item.IsRead)
                };
            })
            .OrderByDescending(item => item.LastMessageAt)
            .ToList();

        return Result<List<ConversationDto>>.Success(conversations);
    }

    public async Task<int> GetUnreadMessageCountAsync(Guid userId)
        => await _dbContext.Messages.CountAsync(item => item.RecipientUserId == userId && !item.IsRead);

    public async Task<Result> MarkConversationAsReadAsync(Guid userId, Guid otherUserId)
    {
        var messages = await ConversationQuery(userId, otherUserId)
            .Where(item => item.RecipientUserId == userId && !item.IsRead)
            .ToListAsync();

        foreach (var message in messages)
        {
            message.IsRead = true;
        }

        await _dbContext.SaveChangesAsync();
        return Result.Success();
    }

    private IQueryable<Message> ConversationQuery(Guid userId, Guid otherUserId)
        => _dbContext.Messages
            .Include(item => item.SenderUser)
            .Include(item => item.RecipientUser)
            .Where(item =>
                (item.SenderUserId == userId && item.RecipientUserId == otherUserId) ||
                (item.SenderUserId == otherUserId && item.RecipientUserId == userId));

    private static MessageDto Map(Message message, Guid currentUserId)
        => new()
        {
            MessageId = message.MessageId,
            SenderUserId = message.SenderUserId,
            RecipientUserId = message.RecipientUserId,
            SenderName = message.SenderUser.Name,
            MessageText = message.MessageText,
            SentAt = message.SentAt,
            IsRead = message.IsRead,
            IsMine = message.SenderUserId == currentUserId
        };
}
