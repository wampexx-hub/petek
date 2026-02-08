using Petek.Shared.Enums;

namespace Petek.Server.Data.Entities;

/// <summary>
/// Kullanıcı entity
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Offline;
    public string? StatusMessage { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    // Şifre ile giriş
    public string? PasswordHash { get; set; }

    // AD bilgileri
    public string? AdObjectGuid { get; set; }
    public string? AdDistinguishedName { get; set; }

    // Navigation properties
    public virtual ICollection<ConversationParticipant> Conversations { get; set; } = new List<ConversationParticipant>();
    public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    public virtual ICollection<ContactFolder> ContactFolders { get; set; } = new List<ContactFolder>();
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
