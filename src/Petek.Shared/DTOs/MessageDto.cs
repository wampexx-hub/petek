using Petek.Shared.Enums;

namespace Petek.Shared.DTOs;

/// <summary>
/// Mesaj veri transfer nesnesi
/// </summary>
public class MessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public MessageStatus Status { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public FileAttachmentDto? Attachment { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
}

/// <summary>
/// Yeni mesaj gönderme
/// </summary>
public class SendMessageDto
{
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public Guid? ReplyToMessageId { get; set; }
}

/// <summary>
/// Mesaj düzenleme
/// </summary>
public class EditMessageDto
{
    public Guid MessageId { get; set; }
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Mesaj okundu bildirimi
/// </summary>
public class MessageReadDto
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public DateTime ReadAt { get; set; }
}

/// <summary>
/// Yazıyor bildirimi
/// </summary>
public class TypingIndicatorDto
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool IsTyping { get; set; }
}
