using Petek.Shared.Enums;

namespace Petek.Shared.DTOs;

/// <summary>
/// Kimlik doğrulama sonucu
/// </summary>
public class AuthResultDto
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserDto? User { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Windows kimlik bilgileri ile giriş
/// </summary>
public class WindowsLoginDto
{
    public string? Domain { get; set; }
    public string Username { get; set; } = string.Empty;
}

/// <summary>
/// Kullanıcı adı ve şifre ile giriş
/// </summary>
public class CredentialsLoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Token yenileme
/// </summary>
public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Oturum bilgisi
/// </summary>
public class SessionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public bool IsCurrent { get; set; }
}
