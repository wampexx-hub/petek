namespace Petek.Shared.Models;

/// <summary>
/// SignalR hub ve metot sabitleri
/// </summary>
public static class SignalRConstants
{
    public const string HubPath = "/hubs/message";

    public static class Methods
    {
        // Server -> Client
        public const string ReceiveMessage = nameof(ReceiveMessage);
        public const string MessageStatusUpdated = nameof(MessageStatusUpdated);
        public const string UserStatusChanged = nameof(UserStatusChanged);
        public const string UserTyping = nameof(UserTyping);
        public const string ConversationCreated = nameof(ConversationCreated);
        public const string ConversationUpdated = nameof(ConversationUpdated);
        public const string UserAddedToConversation = nameof(UserAddedToConversation);
        public const string UserRemovedFromConversation = nameof(UserRemovedFromConversation);
        public const string UserOnline = nameof(UserOnline);
        public const string UserOffline = nameof(UserOffline);
        public const string MessageEdited = nameof(MessageEdited);
        public const string MessageDeleted = nameof(MessageDeleted);
        public const string FileUploadProgress = nameof(FileUploadProgress);
        public const string SessionTerminated = nameof(SessionTerminated);
        public const string ReceiveNotification = nameof(ReceiveNotification);

        // Client -> Server
        public const string SendMessage = nameof(SendMessage);
        public const string EditMessage = nameof(EditMessage);
        public const string DeleteMessage = nameof(DeleteMessage);
        public const string MarkAsRead = nameof(MarkAsRead);
        public const string SetTyping = nameof(SetTyping);
        public const string UpdateStatus = nameof(UpdateStatus);
        public const string JoinConversation = nameof(JoinConversation);
        public const string LeaveConversation = nameof(LeaveConversation);
    }

    public static class Groups
    {
        public static string Conversation(Guid conversationId) => $"conversation_{conversationId}";
        public static string User(Guid userId) => $"user_{userId}";
        public static string Department(string department) => $"department_{department}";
        public static string Admins => "admins";
    }
}
