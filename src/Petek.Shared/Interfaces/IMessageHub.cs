using Petek.Shared.DTOs;
using Petek.Shared.Enums;

namespace Petek.Shared.Interfaces;

/// <summary>
/// SignalR mesaj hub'ı istemci metotları
/// </summary>
public interface IMessageHubClient
{
    /// <summary>Yeni mesaj alındı</summary>
    Task ReceiveMessage(MessageDto message);

    /// <summary>Mesaj durumu güncellendi</summary>
    Task MessageStatusUpdated(Guid messageId, MessageStatus status, DateTime? timestamp);

    /// <summary>Kullanıcı durumu değişti</summary>
    Task UserStatusChanged(Guid userId, UserStatus status);

    /// <summary>Kullanıcı yazıyor</summary>
    Task UserTyping(TypingIndicatorDto indicator);

    /// <summary>Yeni sohbet oluşturuldu</summary>
    Task ConversationCreated(ConversationDto conversation);

    /// <summary>Sohbet güncellendi</summary>
    Task ConversationUpdated(ConversationDto conversation);

    /// <summary>Sohbete kullanıcı eklendi</summary>
    Task UserAddedToConversation(Guid conversationId, ConversationParticipantDto participant);

    /// <summary>Sohbetten kullanıcı çıkarıldı</summary>
    Task UserRemovedFromConversation(Guid conversationId, Guid userId);

    /// <summary>Kullanıcı çevrimiçi oldu</summary>
    Task UserOnline(Guid userId);

    /// <summary>Kullanıcı çevrimdışı oldu</summary>
    Task UserOffline(Guid userId);

    /// <summary>Mesaj düzenlendi</summary>
    Task MessageEdited(MessageDto message);

    /// <summary>Mesaj silindi</summary>
    Task MessageDeleted(Guid conversationId, Guid messageId);

    /// <summary>Dosya yükleme ilerleme durumu</summary>
    Task FileUploadProgress(Guid uploadId, int progress);

    /// <summary>Oturum sonlandırıldı (başka cihazdan)</summary>
    Task SessionTerminated(string reason);

    /// <summary>Admin bildirimi alındı</summary>
    Task ReceiveNotification(string title, string message);
}
