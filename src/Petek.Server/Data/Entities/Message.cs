using Petek.Shared.Enums;

namespace Petek.Server.Data.Entities;

/// <summary>
/// Mesaj entity
/// </summary>
public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? EncryptedContent { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? ReplyToMessageId { get; set; }

    // Navigation properties
    public virtual Conversation Conversation { get; set; } = null!;
    public virtual User Sender { get; set; } = null!;
    public virtual Message? ReplyToMessage { get; set; }
    public virtual FileAttachment? Attachment { get; set; }
    public virtual ICollection<MessageReceipt> Receipts { get; set; } = new List<MessageReceipt>();
}

/// <summary>
/// Mesaj okundu bilgisi entity
/// </summary>
public class MessageReceipt
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public MessageStatus Status { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public virtual Message Message { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
