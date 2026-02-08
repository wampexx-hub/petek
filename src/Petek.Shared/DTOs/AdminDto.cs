using Petek.Shared.Enums;

namespace Petek.Shared.DTOs;

/// <summary>
/// Dashboard istatistikleri
/// </summary>
public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int OnlineUsers { get; set; }
    public long TodayMessageCount { get; set; }
    public long TotalMessageCount { get; set; }
    public long TodayFileTransferBytes { get; set; }
    public int ActiveSessions { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Kullanıcı yönetimi
/// </summary>
public class AdminUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Title { get; set; }
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int ActiveSessionCount { get; set; }
}

/// <summary>
/// Kullanıcı rolü güncelleme
/// </summary>
public class UpdateUserRoleDto
{
    public Guid UserId { get; set; }
    public UserRole Role { get; set; }
}

/// <summary>
/// Surveillance webhook yapılandırması
/// </summary>
public class SurveillanceConfigDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public bool IsEnabled { get; set; }
    public bool CaptureMessages { get; set; }
    public bool CaptureFiles { get; set; }
    public bool CaptureUserActivity { get; set; }
    public DateTime? LastSuccessfulSync { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Surveillance webhook oluşturma/güncelleme
/// </summary>
public class SaveSurveillanceConfigDto
{
    public string Name { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public bool IsEnabled { get; set; }
    public bool CaptureMessages { get; set; }
    public bool CaptureFiles { get; set; }
    public bool CaptureUserActivity { get; set; }
}

/// <summary>
/// Sistem ayarları
/// </summary>
public class SystemSettingsDto
{
    public long MaxFileSizeBytes { get; set; }
    public List<string> AllowedFileExtensions { get; set; } = new();
    public int MaxGroupParticipants { get; set; }
    public int SessionTimeoutMinutes { get; set; }
    public int InactivityTimeoutMinutes { get; set; }
    public bool RequireWindowsAuth { get; set; }
    public bool EnableMessageEncryption { get; set; }
    public int MessageRetentionDays { get; set; }
}

/// <summary>
/// Aktif oturum bilgisi
/// </summary>
public class ActiveSessionDto
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
}

/// <summary>
/// Audit log kaydı
/// </summary>
public class AuditLogDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}

/// <summary>
/// Kullanici olusturma
/// </summary>
public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
}

/// <summary>
/// Kullanici guncelleme
/// </summary>
public class UpdateUserDto
{
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public UserRole? Role { get; set; }
}

/// <summary>
/// Veritabani bilgisi
/// </summary>
public class DatabaseInfoDto
{
    public string Type { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public int UserCount { get; set; }
    public long MessageCount { get; set; }
    public string DatabaseSize { get; set; } = "-";
}

/// <summary>
/// Veritabani yapilandirmasi
/// </summary>
public class DatabaseConfigDto
{
    public string Type { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}

/// <summary>
/// Bildirim gonderme
/// </summary>
public class SendNotificationDto
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool TargetAll { get; set; }
    public List<Guid>? UserIds { get; set; }
}

/// <summary>
/// Sifre belirleme
/// </summary>
public class SetPasswordDto
{
    public string Password { get; set; } = string.Empty;
}
