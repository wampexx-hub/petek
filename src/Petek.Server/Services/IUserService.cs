using Petek.Shared.DTOs;
using Petek.Shared.Enums;

namespace Petek.Server.Services;

public interface IUserService
{
    Task<UserDto?> GetUserAsync(Guid userId);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<List<UserDto>> GetUsersAsync();
    Task<List<UserDto>> SearchUsersAsync(string query);
    Task<List<DepartmentDto>> GetDepartmentsAsync();
    Task<UserDto?> CreateOrUpdateFromAdAsync(string username, string? domain);
    Task<bool> UpdateStatusAsync(Guid userId, UserStatus status);
    Task<bool> SetUserOnlineAsync(Guid userId, bool isOnline);
    Task<bool> UpdateProfileAsync(Guid userId, UpdateUserProfileDto dto);
}

public interface IMessageService
{
    Task<MessageDto?> SendMessageAsync(Guid senderId, SendMessageDto dto);
    Task<MessageDto?> EditMessageAsync(Guid userId, EditMessageDto dto);
    Task<Guid?> DeleteMessageAsync(Guid userId, Guid messageId);
    Task<DateTime?> MarkAsReadAsync(Guid userId, Guid conversationId, Guid messageId);
    Task<List<MessageDto>> GetMessagesAsync(Guid conversationId, int skip = 0, int take = 50);
    Task<MessageDto?> GetMessageAsync(Guid messageId);
}

public interface IConversationService
{
    Task<ConversationDto?> GetConversationAsync(Guid conversationId, Guid userId);
    Task<List<ConversationSummaryDto>> GetUserConversationsAsync(Guid userId);
    Task<ConversationDto?> CreateConversationAsync(Guid userId, CreateConversationDto dto);
    Task<ConversationDto?> UpdateConversationAsync(Guid userId, Guid conversationId, UpdateConversationDto dto);
    Task<bool> AddParticipantAsync(Guid conversationId, Guid userId, Guid addedByUserId);
    Task<bool> RemoveParticipantAsync(Guid conversationId, Guid userId, Guid removedByUserId);
    Task<bool> IsParticipantAsync(Guid conversationId, Guid userId);
    Task<ConversationDto?> GetOrCreateDirectConversationAsync(Guid userId1, Guid userId2);
}

public interface IContactService
{
    Task<List<ContactDto>> GetContactsAsync(Guid userId);
    Task<List<ContactFolderDto>> GetFoldersAsync(Guid userId);
    Task<ContactDto?> AddContactAsync(Guid userId, AddContactDto dto);
    Task<bool> RemoveContactAsync(Guid userId, Guid contactUserId);
    Task<ContactFolderDto?> CreateFolderAsync(Guid userId, string name);
    Task<bool> DeleteFolderAsync(Guid userId, Guid folderId);
    Task<bool> SetFavoriteAsync(Guid userId, Guid contactUserId, bool isFavorite);
    Task<bool> MoveToFolderAsync(Guid userId, Guid contactUserId, Guid? folderId);
}

public interface IConnectionManager
{
    Task AddConnectionAsync(Guid userId, string connectionId);
    Task RemoveConnectionAsync(Guid userId, string connectionId);
    Task<bool> HasConnectionsAsync(Guid userId);
    Task<IEnumerable<string>> GetConnectionsAsync(Guid userId);
}

public interface IAuthService
{
    Task<AuthResultDto> AuthenticateWithWindowsAsync(string username, string? domain);
    Task<AuthResultDto> AuthenticateWithCredentialsAsync(string username, string password);
    Task<AuthResultDto> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeSessionAsync(Guid sessionId);
    Task<List<SessionDto>> GetUserSessionsAsync(Guid userId);
}

public interface IFileService
{
    Task<FileUploadResultDto> UploadFileAsync(Guid userId, Stream fileStream, string fileName, string contentType);
    Task<Stream?> DownloadFileAsync(Guid fileId, Guid userId);
    Task<bool> DeleteFileAsync(Guid fileId, Guid userId);
    Task<FilePolicyDto> GetFilePolicyAsync();
}

public interface ISurveillanceService
{
    Task<bool> SendMessageEventAsync(MessageDto message);
    Task<bool> SendFileEventAsync(FileAttachmentDto file, Guid senderId);
    Task<bool> SendUserActivityEventAsync(Guid userId, string activity);
    Task<List<SurveillanceConfigDto>> GetConfigsAsync();
    Task<SurveillanceConfigDto?> SaveConfigAsync(SaveSurveillanceConfigDto dto);
    Task<bool> TestWebhookAsync(Guid configId);
}
