using Petek.Shared.Enums;

namespace Petek.Server.Data.Entities;

/// <summary>
/// Sohbet entity
/// </summary>
public class Conversation
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
    public ConversationType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActivityAt { get; set; }
    public Guid CreatedByUserId { get; set; }

    // Navigation properties
    public virtual ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}

/// <summary>
/// Sohbet katılımcısı entity
/// </summary>
public class ConversationParticipant
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsMuted { get; set; }
    public bool IsPinned { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public DateTime? LastReadAt { get; set; }
    public Guid? LastReadMessageId { get; set; }

    // Navigation properties
    public virtual Conversation Conversation { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
