using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sakanak.BLL.DTOs.Messages;
using Sakanak.BLL.Interfaces;
using Sakanak.Domain.Entities;

namespace Sakanak.Web.Controllers;

[Authorize]
public class MessagesController : Controller
{
    private readonly IMessageService _messageService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MessagesController(IMessageService messageService, UserManager<ApplicationUser> userManager)
    {
        _messageService = messageService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? otherUserId = null)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Challenge();

        var conversationsResult = await _messageService.GetUserConversationsAsync(userId.Value);
        var conversations = conversationsResult.Succeeded ? conversationsResult.Data! : new List<ConversationDto>();

        var adminUser = (await _userManager.GetUsersInRoleAsync("Admin")).FirstOrDefault();
        
        if (adminUser != null && userId.Value != adminUser.Id)
        {
            var adminConversation = conversations.FirstOrDefault(c => c.OtherUserId == adminUser.Id);
            if (adminConversation == null)
            {
                conversations.Add(new ConversationDto
                {
                    OtherUserId = adminUser.Id,
                    OtherUserName = adminUser.Name,
                    LastMessagePreview = "Admin Support",
                    LastMessageAt = DateTime.MinValue,
                    UnreadCount = 0,
                    IsAdminConversation = true
                });
            }
            else
            {
                adminConversation.IsAdminConversation = true;
            }
        }

        if (otherUserId.HasValue && !conversations.Any(c => c.OtherUserId == otherUserId.Value))
        {
            var otherUser = await _userManager.FindByIdAsync(otherUserId.Value.ToString());
            if (otherUser != null)
            {
                conversations.Add(new ConversationDto
                {
                    OtherUserId = otherUser.Id,
                    OtherUserName = otherUser.Name,
                    LastMessagePreview = "Start a conversation",
                    LastMessageAt = DateTime.UtcNow,
                    UnreadCount = 0,
                    IsAdminConversation = otherUser.Id == adminUser?.Id
                });
            }
        }

        conversations = conversations
            .OrderByDescending(c => c.IsAdminConversation)
            .ThenByDescending(c => c.LastMessageAt)
            .ToList();

        var selectedUserId = otherUserId ?? conversations.FirstOrDefault()?.OtherUserId;
        if (selectedUserId.HasValue)
        {
            await _messageService.MarkConversationAsReadAsync(userId.Value, selectedUserId.Value);
            var messages = await _messageService.GetConversationAsync(userId.Value, selectedUserId.Value);
            ViewBag.Messages = messages.Succeeded ? messages.Data! : new List<Sakanak.BLL.DTOs.Messages.MessageDto>();
            ViewBag.SelectedUserId = selectedUserId.Value;
        }
        else
        {
            ViewBag.Messages = new List<Sakanak.BLL.DTOs.Messages.MessageDto>();
        }

        return View(conversations);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(Guid recipientUserId, string messageText, string? recipientType = null, int? relatedEntityId = null, string? relatedEntityType = null)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Challenge();

        var senderType = User.IsInRole("Student") ? "Student" : User.IsInRole("Landlord") ? "Landlord" : "Admin";
        var result = await _messageService.SendMessageAsync(userId.Value, recipientUserId, messageText, senderType, recipientType ?? "User", relatedEntityId, relatedEntityType);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? "Message sent." : result.ErrorMessage;
        return RedirectToAction(nameof(Index), new { otherUserId = recipientUserId });
    }

    private Guid? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var parsed) ? parsed : null;
    }
}
