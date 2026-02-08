using Petek.Server.Data;
using Petek.Server.Data.Entities;
using Petek.Shared.DTOs;
using Petek.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Petek.Server.Services;

public class MessageService : IMessageService
{
    private readonly PetekDbContext _context;
    private readonly ISurveillanceService _surveillanceService;
    private readonly ILogger<MessageService> _logger;

    public MessageService(
        PetekDbContext context,
        ISurveillanceService surveillanceService,
        ILogger<MessageService> logger)
    {
        _context = context;
        _surveillanceService = surveillanceService;
        _logger = logger;
    }

    public async Task<MessageDto?> SendMessageAsync(Guid senderId, SendMessageDto dto)
    {
        // Kullanıcının sohbete katılımcı olduğunu doğrula
        var isParticipant = await _context.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == dto.ConversationId &&
                           cp.UserId == senderId &&
                           cp.LeftAt == null);

        if (!isParticipant) return null;

        var sender = await _context.Users.FindAsync(senderId);
        if (sender == null) return null;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = dto.ConversationId,
            SenderId = senderId,
            Content = dto.Content,
            Type = dto.Type,
            SentAt = DateTime.UtcNow,
            ReplyToMessageId = dto.ReplyToMessageId
        };

        _context.Messages.Add(message);

        // Sohbet son aktivite zamanını güncelle
        var conversation = await _context.Conversations.FindAsync(dto.ConversationId);
        if (conversation != null)
        {
            conversation.LastActivityAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var messageDto = new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderName = sender.DisplayName,
            SenderAvatarUrl = sender.AvatarUrl,
            Content = message.Content,
            Type = message.Type,
            Status = MessageStatus.Sent,
            SentAt = message.SentAt,
            ReplyToMessageId = message.ReplyToMessageId
        };

        // Surveillance servisine bildir
        await _surveillanceService.SendMessageEventAsync(messageDto);

        return messageDto;
    }

    public async Task<MessageDto?> EditMessageAsync(Guid userId, EditMessageDto dto)
    {
        var message = await _context.Messages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == dto.MessageId);

        if (message == null || message.SenderId != userId || message.IsDeleted)
            return null;

        message.Content = dto.Content;
        message.IsEdited = true;
        message.EditedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderName = message.Sender.DisplayName,
            SenderAvatarUrl = message.Sender.AvatarUrl,
            Content = message.Content,
            Type = message.Type,
            SentAt = message.SentAt,
            IsEdited = true,
            EditedAt = message.EditedAt
        };
    }

    public async Task<Guid?> DeleteMessageAsync(Guid userId, Guid messageId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null || message.SenderId != userId)
            return null;

        message.IsDeleted = true;
        message.DeletedAt = DateTime.UtcNow;
        message.Content = string.Empty;

        await _context.SaveChangesAsync();
        return message.ConversationId;
    }

    public async Task<DateTime?> MarkAsReadAsync(Guid userId, Guid conversationId, Guid messageId)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

        if (participant == null) return null;

        var readAt = DateTime.UtcNow;
        participant.LastReadAt = readAt;
        participant.LastReadMessageId = messageId;

        // Mesaj receipt'i güncelle
        var receipt = await _context.MessageReceipts
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);

        if (receipt == null)
        {
            receipt = new MessageReceipt
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                UserId = userId,
                Status = MessageStatus.Read,
                ReadAt = readAt
            };
            _context.MessageReceipts.Add(receipt);
        }
        else
        {
            receipt.Status = MessageStatus.Read;
            receipt.ReadAt = readAt;
        }

        await _context.SaveChangesAsync();
        return readAt;
    }

    public async Task<List<MessageDto>> GetMessagesAsync(Guid conversationId, int skip = 0, int take = 50)
    {
        return await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Attachment)
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                SenderName = m.Sender.DisplayName,
                SenderAvatarUrl = m.Sender.AvatarUrl,
                Content = m.Content,
                Type = m.Type,
                Status = MessageStatus.Sent,
                SentAt = m.SentAt,
                IsEdited = m.IsEdited,
                EditedAt = m.EditedAt,
                ReplyToMessageId = m.ReplyToMessageId,
                Attachment = m.Attachment != null ? new FileAttachmentDto
                {
                    Id = m.Attachment.Id,
                    FileName = m.Attachment.FileName,
                    FileExtension = m.Attachment.FileExtension,
                    ContentType = m.Attachment.ContentType,
                    FileSize = m.Attachment.FileSize,
                    UploadedAt = m.Attachment.UploadedAt
                } : null
            })
            .ToListAsync();
    }

    public async Task<MessageDto?> GetMessageAsync(Guid messageId)
    {
        var message = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Attachment)
            .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);

        if (message == null) return null;

        return new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderName = message.Sender.DisplayName,
            SenderAvatarUrl = message.Sender.AvatarUrl,
            Content = message.Content,
            Type = message.Type,
            SentAt = message.SentAt,
            IsEdited = message.IsEdited,
            EditedAt = message.EditedAt
        };
    }
}
