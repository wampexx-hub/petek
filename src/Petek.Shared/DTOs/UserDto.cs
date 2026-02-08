using Petek.Shared.Enums;

namespace Petek.Shared.DTOs;

/// <summary>
/// Kullanıcı veri transfer nesnesi
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; }
    public UserRole Role { get; set; }
    public DateTime LastSeen { get; set; }
    public bool IsOnline { get; set; }
}

/// <summary>
/// Kullanıcı profil güncelleme
/// </summary>
public class UpdateUserProfileDto
{
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public UserStatus? Status { get; set; }
}

/// <summary>
/// Kullanıcı durum güncelleme
/// </summary>
public class UpdateUserStatusDto
{
    public UserStatus Status { get; set; }
    public string? StatusMessage { get; set; }
}
