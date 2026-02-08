using Petek.Server.Data;
using Petek.Server.Data.Entities;
using Petek.Shared.DTOs;
using Petek.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Petek.Server.Services;

public class ConversationService : IConversationService
{
    private readonly PetekDbContext _context;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(PetekDbContext context, ILogger<ConversationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ConversationDto?> GetConversationAsync(Guid conversationId, Guid userId)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null) return null;

        var participant = conversation.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null || participant.LeftAt != null) return null;

        var lastMessage = await _context.Messages
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
            .OrderByDescending(m => m.SentAt)
            .FirstOrDefaultAsync();

        return MapToDto(conversation, userId, lastMessage);
    }

    public async Task<List<ConversationSummaryDto>> GetUserConversationsAsync(Guid userId)
    {
        var participations = await _context.ConversationParticipants
            .Include(cp => cp.Conversation)
                .ThenInclude(c => c.Participants)
                    .ThenInclude(p => p.User)
            .Where(cp => cp.UserId == userId && cp.LeftAt == null)
            .ToListAsync();

        var result = new List<ConversationSummaryDto>();

        foreach (var participation in participations)
        {
            var conv = participation.Conversation;
            var lastMessage = await _context.Messages
                .Where(m => m.ConversationId == conv.Id && !m.IsDeleted)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();

            var unreadCount = await _context.Messages
                .CountAsync(m => m.ConversationId == conv.Id &&
                                !m.IsDeleted &&
                                m.SenderId != userId &&
                                m.SentAt > (participation.LastReadAt ?? DateTime.MinValue));

            var summary = new ConversationSummaryDto
            {
                Id = conv.Id,
                Name = GetConversationName(conv, userId),
                AvatarUrl = GetConversationAvatar(conv, userId),
                Type = conv.Type,
                LastMessageContent = lastMessage?.Content,
                LastMessageAt = lastMessage?.SentAt,
                UnreadCount = unreadCount,
                IsMuted = participation.IsMuted,
                IsPinned = participation.IsPinned
            };

            if (conv.Type == ConversationType.Direct)
            {
                var otherUser = conv.Participants.FirstOrDefault(p => p.UserId != userId)?.User;
                if (otherUser != null)
                {
                    summary.OtherUserStatus = otherUser.Status;
                }
            }

            result.Add(summary);
        }

        return result
            .OrderByDescending(c => c.IsPinned)
            .ThenByDescending(c => c.LastMessageAt)
            .ToList();
    }

    public async Task<ConversationDto?> CreateConversationAsync(Guid userId, CreateConversationDto dto)
    {
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Type = dto.Type,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        _context.Conversations.Add(conversation);

        // Oluşturan kullanıcıyı ekle
        var creatorParticipant = new ConversationParticipant
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            UserId = userId,
            IsAdmin = true,
            JoinedAt = DateTime.UtcNow
        };
        _context.ConversationParticipants.Add(creatorParticipant);

        // Diğer katılımcıları ekle
        foreach (var participantId in dto.ParticipantIds.Where(id => id != userId))
        {
            var participant = new ConversationParticipant
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                UserId = participantId,
                IsAdmin = false,
                JoinedAt = DateTime.UtcNow
            };
            _context.ConversationParticipants.Add(participant);
        }

        await _context.SaveChangesAsync();

        return await GetConversationAsync(conversation.Id, userId);
    }

    public async Task<ConversationDto?> UpdateConversationAsync(Guid userId, Guid conversationId, UpdateConversationDto dto)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null) return null;

        var participant = conversation.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null || !participant.IsAdmin) return null;

        if (dto.Name != null) conversation.Name = dto.Name;
        if (dto.AvatarUrl != null) conversation.AvatarUrl = dto.AvatarUrl;

        await _context.SaveChangesAsync();
        return await GetConversationAsync(conversationId, userId);
    }

    public async Task<bool> AddParticipantAsync(Guid conversationId, Guid userId, Guid addedByUserId)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null || conversation.Type != ConversationType.Group) return false;

        var addedBy = conversation.Participants.FirstOrDefault(p => p.UserId == addedByUserId);
        if (addedBy == null || !addedBy.IsAdmin) return false;

        var existingParticipant = conversation.Participants.FirstOrDefault(p => p.UserId == userId);
        if (existingParticipant != null)
        {
            if (existingParticipant.LeftAt != null)
            {
                existingParticipant.LeftAt = null;
                existingParticipant.JoinedAt = DateTime.UtcNow;
            }
            else
            {
                return false;
            }
        }
        else
        {
            var participant = new ConversationParticipant
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                UserId = userId,
                IsAdmin = false,
                JoinedAt = DateTime.UtcNow
            };
            _context.ConversationParticipants.Add(participant);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveParticipantAsync(Guid conversationId, Guid userId, Guid removedByUserId)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null) return false;

        var removedBy = conversation.Participants.FirstOrDefault(p => p.UserId == removedByUserId);
        if (removedBy == null || (!removedBy.IsAdmin && removedByUserId != userId)) return false;

        var participant = conversation.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null || participant.LeftAt != null) return false;

        participant.LeftAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsParticipantAsync(Guid conversationId, Guid userId)
    {
        return await _context.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId &&
                           cp.UserId == userId &&
                           cp.LeftAt == null);
    }

    public async Task<ConversationDto?> GetOrCreateDirectConversationAsync(Guid userId1, Guid userId2)
    {
        // Mevcut direkt sohbeti bul
        var existingConversation = await _context.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Where(c => c.Type == ConversationType.Direct)
            .Where(c => c.Participants.Any(p => p.UserId == userId1 && p.LeftAt == null) &&
                       c.Participants.Any(p => p.UserId == userId2 && p.LeftAt == null))
            .FirstOrDefaultAsync();

        if (existingConversation != null)
        {
            return MapToDto(existingConversation, userId1, null);
        }

        // Yeni direkt sohbet oluştur
        return await CreateConversationAsync(userId1, new CreateConversationDto
        {
            Type = ConversationType.Direct,
            ParticipantIds = new List<Guid> { userId1, userId2 }
        });
    }

    private ConversationDto MapToDto(Conversation conversation, Guid currentUserId, Message? lastMessage)
    {
        var currentParticipant = conversation.Participants.FirstOrDefault(p => p.UserId == currentUserId);

        return new ConversationDto
        {
            Id = conversation.Id,
            Name = GetConversationName(conversation, currentUserId),
            AvatarUrl = GetConversationAvatar(conversation, currentUserId),
            Type = conversation.Type,
            Participants = conversation.Participants
                .Where(p => p.LeftAt == null)
                .Select(p => new ConversationParticipantDto
                {
                    UserId = p.UserId,
                    DisplayName = p.User.DisplayName,
                    AvatarUrl = p.User.AvatarUrl,
                    Status = p.User.Status,
                    IsAdmin = p.IsAdmin,
                    JoinedAt = p.JoinedAt
                }).ToList(),
            LastMessage = lastMessage != null ? new MessageDto
            {
                Id = lastMessage.Id,
                ConversationId = lastMessage.ConversationId,
                SenderId = lastMessage.SenderId,
                Content = lastMessage.Content,
                Type = lastMessage.Type,
                SentAt = lastMessage.SentAt
            } : null,
            CreatedAt = conversation.CreatedAt,
            LastActivityAt = conversation.LastActivityAt,
            IsMuted = currentParticipant?.IsMuted ?? false,
            IsPinned = currentParticipant?.IsPinned ?? false
        };
    }

    private string GetConversationName(Conversation conversation, Guid currentUserId)
    {
        if (!string.IsNullOrEmpty(conversation.Name))
            return conversation.Name;

        if (conversation.Type == ConversationType.Direct)
        {
            var otherUser = conversation.Participants.FirstOrDefault(p => p.UserId != currentUserId)?.User;
            return otherUser?.DisplayName ?? "Bilinmeyen";
        }

        return "Grup Sohbeti";
    }

    private string? GetConversationAvatar(Conversation conversation, Guid currentUserId)
    {
        if (!string.IsNullOrEmpty(conversation.AvatarUrl))
            return conversation.AvatarUrl;

        if (conversation.Type == ConversationType.Direct)
        {
            var otherUser = conversation.Participants.FirstOrDefault(p => p.UserId != currentUserId)?.User;
            return otherUser?.AvatarUrl;
        }

        return null;
    }
}
