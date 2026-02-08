using Petek.Shared.Enums;

namespace Petek.Shared.DTOs;

/// <summary>
/// Sohbet veri transfer nesnesi
/// </summary>
public class ConversationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public ConversationType Type { get; set; }
    public List<ConversationParticipantDto> Participants { get; set; } = new();
    public MessageDto? LastMessage { get; set; }
    public int UnreadCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public bool IsMuted { get; set; }
    public bool IsPinned { get; set; }
}

/// <summary>
/// Sohbet katılımcısı
/// </summary>
public class ConversationParticipantDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime JoinedAt { get; set; }
}

/// <summary>
/// Yeni sohbet oluşturma
/// </summary>
public class CreateConversationDto
{
    public string? Name { get; set; }
    public ConversationType Type { get; set; }
    public List<Guid> ParticipantIds { get; set; } = new();
}

/// <summary>
/// Grup sohbeti güncelleme
/// </summary>
public class UpdateConversationDto
{
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// Sohbet özeti (liste için)
/// </summary>
public class ConversationSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public ConversationType Type { get; set; }
    public string? LastMessageContent { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public bool IsMuted { get; set; }
    public bool IsPinned { get; set; }
    public UserStatus? OtherUserStatus { get; set; }
}
