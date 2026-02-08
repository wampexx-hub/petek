namespace Petek.Server.Data.Entities;

/// <summary>
/// Surveillance (DLP/Gözetim) yapılandırması entity
/// </summary>
public class SurveillanceConfig
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public bool IsEnabled { get; set; }
    public bool CaptureMessages { get; set; } = true;
    public bool CaptureFiles { get; set; } = true;
    public bool CaptureUserActivity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSuccessfulSync { get; set; }
    public string? LastError { get; set; }
    public Guid CreatedByUserId { get; set; }
}

/// <summary>
/// Audit log entity
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// Sistem ayarları entity
/// </summary>
public class SystemSettings
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
}
