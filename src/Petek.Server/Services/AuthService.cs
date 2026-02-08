using Petek.Server.Data;
using Petek.Server.Data.Entities;
using Petek.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Petek.Server.Services;

public class AuthService : IAuthService
{
    private readonly PetekDbContext _context;
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        PetekDbContext context,
        IUserService userService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _userService = userService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResultDto> AuthenticateWithWindowsAsync(string username, string? domain)
    {
        try
        {
            // Kullanıcıyı AD'den oluştur veya güncelle
            var user = await _userService.CreateOrUpdateFromAdAsync(username, domain);
            if (user == null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    ErrorMessage = "Kullanıcı oluşturulamadı"
                };
            }

            // Token oluştur
            var token = GenerateToken();
            var refreshToken = GenerateToken();
            var expiresAt = DateTime.UtcNow.AddHours(24);

            // Oturum kaydet
            var session = new Session
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = token,
                RefreshToken = refreshToken,
                DeviceName = Environment.MachineName,
                IpAddress = "127.0.0.1",
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                IsActive = true
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return new AuthResultDto
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = user
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for user {Username}", username);
            return new AuthResultDto
            {
                Success = false,
                ErrorMessage = $"Kimlik doğrulama hatası: {ex.Message} {(ex.InnerException != null ? " -> " + ex.InnerException.Message : "")}"
            };
        }
    }

    public async Task<AuthResultDto> AuthenticateWithCredentialsAsync(string username, string password)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower() && u.IsEnabled);

            if (user == null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    ErrorMessage = "Kullanıcı adı veya şifre hatalı"
                };
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                return new AuthResultDto
                {
                    Success = false,
                    ErrorMessage = "Bu hesap için şifre tanımlanmamış. Windows ile giriş yapın veya yöneticinizle iletişime geçin."
                };
            }

            // Verify password
            if (!VerifyPassword(password, user.PasswordHash))
            {
                return new AuthResultDto
                {
                    Success = false,
                    ErrorMessage = "Kullanıcı adı veya şifre hatalı"
                };
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            user.Status = Petek.Shared.Enums.UserStatus.Available;

            // Create token
            var token = GenerateToken();
            var refreshToken = GenerateToken();
            var expiresAt = DateTime.UtcNow.AddHours(24);

            var session = new Session
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = token,
                RefreshToken = refreshToken,
                DeviceName = Environment.MachineName,
                IpAddress = "127.0.0.1",
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                IsActive = true
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Department = user.Department,
                Title = user.Title,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status,
                Role = user.Role
            };

            return new AuthResultDto
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Credential authentication failed for user {Username}", username);
            return new AuthResultDto
            {
                Success = false,
                ErrorMessage = $"Kimlik doğrulama hatası: {ex.Message}"
            };
        }
    }

    public async Task<AuthResultDto> RefreshTokenAsync(string refreshToken)
    {
        var session = await _context.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && s.IsActive);

        if (session == null || session.ExpiresAt < DateTime.UtcNow)
        {
            return new AuthResultDto
            {
                Success = false,
                ErrorMessage = "Geçersiz veya süresi dolmuş token"
            };
        }

        // Yeni token oluştur
        var newToken = GenerateToken();
        var newRefreshToken = GenerateToken();
        var newExpiresAt = DateTime.UtcNow.AddHours(24);

        session.Token = newToken;
        session.RefreshToken = newRefreshToken;
        session.ExpiresAt = newExpiresAt;
        session.LastActivityAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var userDto = new UserDto
        {
            Id = session.User.Id,
            Username = session.User.Username,
            DisplayName = session.User.DisplayName,
            Email = session.User.Email,
            Department = session.User.Department,
            Title = session.User.Title,
            AvatarUrl = session.User.AvatarUrl,
            Status = session.User.Status,
            Role = session.User.Role
        };

        return new AuthResultDto
        {
            Success = true,
            Token = newToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = newExpiresAt,
            User = userDto
        };
    }

    public async Task<bool> RevokeSessionAsync(Guid sessionId)
    {
        var session = await _context.Sessions.FindAsync(sessionId);
        if (session == null) return false;

        session.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<SessionDto>> GetUserSessionsAsync(Guid userId)
    {
        return await _context.Sessions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastActivityAt)
            .Select(s => new SessionDto
            {
                Id = s.Id,
                UserId = s.UserId,
                DeviceName = s.DeviceName,
                IpAddress = s.IpAddress,
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt
            })
            .ToListAsync();
    }

    private static string GenerateToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public static string HashPassword(string password)
    {
        var salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        var combined = new byte[48]; // 16 salt + 32 hash
        Array.Copy(salt, 0, combined, 0, 16);
        Array.Copy(hash, 0, combined, 16, 32);

        return Convert.ToBase64String(combined);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            var combined = Convert.FromBase64String(storedHash);
            if (combined.Length != 48) return false;

            var salt = new byte[16];
            Array.Copy(combined, 0, salt, 0, 16);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            for (int i = 0; i < 32; i++)
            {
                if (combined[i + 16] != hash[i]) return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}
