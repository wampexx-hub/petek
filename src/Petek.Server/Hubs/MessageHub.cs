using Petek.Server.Services;
using Petek.Shared.DTOs;
using Petek.Shared.Enums;
using Petek.Shared.Interfaces;
using Petek.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Petek.Server.Hubs;

/// <summary>
/// Gerçek zamanlı mesajlaşma hub'ı
/// </summary>
[Authorize]
public class MessageHub : Hub<IMessageHubClient>
{
    private readonly IMessageService _messageService;
    private readonly IUserService _userService;
    private readonly IConversationService _conversationService;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<MessageHub> _logger;

    public MessageHub(
        IMessageService messageService,
        IUserService userService,
        IConversationService conversationService,
        IConnectionManager connectionManager,
        ILogger<MessageHub> logger)
    {
        _messageService = messageService;
        _userService = userService;
        _conversationService = conversationService;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            Context.Abort();
            return;
        }

        await _connectionManager.AddConnectionAsync(userId, Context.ConnectionId);
        await _userService.SetUserOnlineAsync(userId, true);

        // Kullanıcının sohbetlerine katıl
        var conversations = await _conversationService.GetUserConversationsAsync(userId);
        foreach (var conv in conversations)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, SignalRConstants.Groups.Conversation(conv.Id));
        }

        // Kullanıcı grubuna katıl
        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRConstants.Groups.User(userId));

        // Diğer kullanıcılara bildir
        await Clients.Others.UserOnline(userId);

        _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId != Guid.Empty)
        {
            await _connectionManager.RemoveConnectionAsync(userId, Context.ConnectionId);

            // Kullanıcının başka bağlantısı yoksa çevrimdışı yap
            if (!await _connectionManager.HasConnectionsAsync(userId))
            {
                await _userService.SetUserOnlineAsync(userId, false);
                await Clients.Others.UserOffline(userId);
            }

            _logger.LogInformation("User {UserId} disconnected", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Mesaj gönder
    /// </summary>
    public async Task SendMessage(SendMessageDto dto)
    {
        var userId = GetUserId();
        var message = await _messageService.SendMessageAsync(userId, dto);

        if (message != null)
        {
            await Clients.Group(SignalRConstants.Groups.Conversation(dto.ConversationId))
                .ReceiveMessage(message);
        }
    }

    /// <summary>
    /// Mesaj düzenle
    /// </summary>
    public async Task EditMessage(EditMessageDto dto)
    {
        var userId = GetUserId();
        var message = await _messageService.EditMessageAsync(userId, dto);

        if (message != null)
        {
            await Clients.Group(SignalRConstants.Groups.Conversation(message.ConversationId))
                .MessageEdited(message);
        }
    }

    /// <summary>
    /// Mesaj sil
    /// </summary>
    public async Task DeleteMessage(Guid messageId)
    {
        var userId = GetUserId();
        var conversationId = await _messageService.DeleteMessageAsync(userId, messageId);

        if (conversationId.HasValue)
        {
            await Clients.Group(SignalRConstants.Groups.Conversation(conversationId.Value))
                .MessageDeleted(conversationId.Value, messageId);
        }
    }

    /// <summary>
    /// Mesajı okundu olarak işaretle
    /// </summary>
    public async Task MarkAsRead(Guid conversationId, Guid messageId)
    {
        var userId = GetUserId();
        var readAt = await _messageService.MarkAsReadAsync(userId, conversationId, messageId);

        if (readAt.HasValue)
        {
            await Clients.Group(SignalRConstants.Groups.Conversation(conversationId))
                .MessageStatusUpdated(messageId, MessageStatus.Read, readAt.Value);
        }
    }

    /// <summary>
    /// Yazıyor durumunu bildir
    /// </summary>
    public async Task SetTyping(Guid conversationId, bool isTyping)
    {
        var userId = GetUserId();
        var user = await _userService.GetUserAsync(userId);

        if (user != null)
        {
            var indicator = new TypingIndicatorDto
            {
                ConversationId = conversationId,
                UserId = userId,
                UserName = user.DisplayName,
                IsTyping = isTyping
            };

            await Clients.OthersInGroup(SignalRConstants.Groups.Conversation(conversationId))
                .UserTyping(indicator);
        }
    }

    /// <summary>
    /// Kullanıcı durumunu güncelle
    /// </summary>
    public async Task UpdateStatus(UserStatus status)
    {
        var userId = GetUserId();
        await _userService.UpdateStatusAsync(userId, status);
        await Clients.Others.UserStatusChanged(userId, status);
    }

    /// <summary>
    /// Sohbete katıl
    /// </summary>
    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetUserId();
        if (await _conversationService.IsParticipantAsync(conversationId, userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, SignalRConstants.Groups.Conversation(conversationId));
        }
    }

    /// <summary>
    /// Sohbetten ayrıl
    /// </summary>
    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRConstants.Groups.Conversation(conversationId));
    }

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst("sub")?.Value
                          ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
