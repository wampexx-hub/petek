using Petek.Server.Data;
using Petek.Server.Data.Entities;
using Petek.Server.Hubs;
using Petek.Server.Services;
using Petek.Shared.DTOs;
using Petek.Shared.Enums;
using Petek.Shared.Interfaces;
using Petek.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Petek.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly PetekDbContext _context;
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminController> _logger;
    private readonly IHubContext<MessageHub, IMessageHubClient> _hubContext;

    public AdminController(
        PetekDbContext context,
        IUserService userService,
        IConfiguration configuration,
        ILogger<AdminController> logger,
        IHubContext<MessageHub, IMessageHubClient> hubContext)
    {
        _context = context;
        _userService = userService;
        _configuration = configuration;
        _logger = logger;
        _hubContext = hubContext;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats()
    {
        var today = DateTime.UtcNow.Date;

        var stats = new DashboardStatsDto
        {
            TotalUsers = await _context.Users.CountAsync(u => u.IsEnabled),
            ActiveUsers = await _context.Users.CountAsync(u => u.IsEnabled && u.LastLoginAt >= today.AddDays(-7)),
            OnlineUsers = await _context.Users.CountAsync(u => u.Status != UserStatus.Offline),
            TodayMessageCount = await _context.Messages.CountAsync(m => m.SentAt >= today),
            TotalMessageCount = await _context.Messages.CountAsync(),
            TodayFileTransferBytes = await _context.FileAttachments
                .Where(f => f.UploadedAt >= today)
                .SumAsync(f => f.FileSize),
            ActiveSessions = await _context.Sessions.CountAsync(s => s.IsActive),
            LastUpdated = DateTime.UtcNow
        };

        return Ok(ApiResponse<DashboardStatsDto>.Ok(stats));
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<List<AdminUserDto>>>> GetUsers()
    {
        var users = await _context.Users
            .Include(u => u.Sessions)
            .OrderBy(u => u.DisplayName)
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                Username = u.Username,
                DisplayName = u.DisplayName,
                Email = u.Email,
                Department = u.Department,
                Title = u.Title,
                Role = u.Role,
                Status = u.Status,
                IsEnabled = u.IsEnabled,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                ActiveSessionCount = u.Sessions.Count(s => s.IsActive)
            })
            .ToListAsync();

        return Ok(ApiResponse<List<AdminUserDto>>.Ok(users));
    }

    [HttpPost("users")]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> CreateUser([FromBody] CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.DisplayName) || string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail("Kullanici adi, gorunen ad ve e-posta zorunludur"));
        }

        var existing = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == dto.Username.ToLower());
        if (existing != null)
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail("Bu kullanici adi zaten kullaniliyor"));
        }

        var emailExists = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
        if (emailExists != null)
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail("Bu e-posta adresi zaten kullaniliyor"));
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = dto.Username,
            DisplayName = dto.DisplayName,
            Email = dto.Email,
            PasswordHash = !string.IsNullOrWhiteSpace(dto.Password) ? AuthService.HashPassword(dto.Password) : null,
            Department = dto.Department,
            Title = dto.Title,
            Role = dto.Role,
            Status = UserStatus.Offline,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await LogAction("CreateUser", "User", user.Id, $"User created: {dto.Username}");

        var result = new AdminUserDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Department = user.Department,
            Title = user.Title,
            Role = user.Role,
            Status = user.Status,
            IsEnabled = user.IsEnabled,
            CreatedAt = user.CreatedAt
        };

        return Ok(ApiResponse<AdminUserDto>.Ok(result, "Kullanici olusturuldu"));
    }

    [HttpPut("users/{userId}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateUser(Guid userId, [FromBody] UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<bool>.Fail("Kullanici bulunamadi"));
        }

        if (!string.IsNullOrWhiteSpace(dto.DisplayName)) user.DisplayName = dto.DisplayName;
        if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email;
        if (dto.Department != null) user.Department = dto.Department;
        if (dto.Title != null) user.Title = dto.Title;
        if (dto.Role.HasValue) user.Role = dto.Role.Value;

        await _context.SaveChangesAsync();

        await LogAction("UpdateUser", "User", userId, $"User updated: {user.Username}");

        return Ok(ApiResponse<bool>.Ok(true, "Kullanici guncellendi"));
    }

    [HttpDelete("users/{userId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<bool>.Fail("Kullanici bulunamadi"));
        }

        var sessions = await _context.Sessions.Where(s => s.UserId == userId).ToListAsync();
        _context.Sessions.RemoveRange(sessions);

        var contacts = await _context.Contacts.Where(c => c.OwnerId == userId || c.ContactUserId == userId).ToListAsync();
        _context.Contacts.RemoveRange(contacts);

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        await LogAction("DeleteUser", "User", userId, $"User deleted: {user.Username}");

        return Ok(ApiResponse<bool>.Ok(true, "Kullanici silindi"));
    }

    [HttpPut("users/{userId}/role")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<bool>.Fail("Kullanici bulunamadi"));
        }

        user.Role = dto.Role;
        await _context.SaveChangesAsync();

        await LogAction("UpdateUserRole", "User", userId, $"Role changed to {dto.Role}");

        return Ok(ApiResponse<bool>.Ok(true, "Kullanici rolu guncellendi"));
    }

    [HttpPut("users/{userId}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> SetUserEnabled(Guid userId, [FromBody] bool isEnabled)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<bool>.Fail("Kullanici bulunamadi"));
        }

        user.IsEnabled = isEnabled;
        await _context.SaveChangesAsync();

        await LogAction("SetUserEnabled", "User", userId, $"Enabled: {isEnabled}");

        return Ok(ApiResponse<bool>.Ok(true, isEnabled ? "Kullanici aktiflestirildi" : "Kullanici devre disi birakildi"));
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<ApiResponse<List<ActiveSessionDto>>>> GetActiveSessions()
    {
        var sessions = await _context.Sessions
            .Include(s => s.User)
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.LastActivityAt)
            .Select(s => new ActiveSessionDto
            {
                SessionId = s.Id,
                UserId = s.UserId,
                Username = s.User.Username,
                DisplayName = s.User.DisplayName,
                DeviceName = s.DeviceName,
                IpAddress = s.IpAddress,
                StartedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<ActiveSessionDto>>.Ok(sessions));
    }

    [HttpDelete("sessions/{sessionId}")]
    public async Task<ActionResult<ApiResponse<bool>>> TerminateSession(Guid sessionId)
    {
        var session = await _context.Sessions.FindAsync(sessionId);
        if (session == null)
        {
            return NotFound(ApiResponse<bool>.Fail("Oturum bulunamadi"));
        }

        session.IsActive = false;
        await _context.SaveChangesAsync();

        await LogAction("TerminateSession", "Session", sessionId, $"Session terminated for user {session.UserId}");

        return Ok(ApiResponse<bool>.Ok(true, "Oturum sonlandirildi"));
    }

    [HttpGet("database/info")]
    public async Task<ActionResult<ApiResponse<DatabaseInfoDto>>> GetDatabaseInfo()
    {
        var dbType = _configuration["Database:Type"] ?? "SQLite";
        var isConnected = false;
        var dbSize = "-";

        try
        {
            await _context.Database.CanConnectAsync();
            isConnected = true;

            if (dbType.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
            {
                var connStr = _configuration.GetConnectionString("SQLite") ?? "Data Source=Petek_v1.db";
                var dbPath = connStr.Replace("Data Source=", "").Trim();
                if (System.IO.File.Exists(dbPath))
                {
                    var fileInfo = new System.IO.FileInfo(dbPath);
                    var sizeMb = fileInfo.Length / (1024.0 * 1024.0);
                    dbSize = sizeMb < 1 ? $"{fileInfo.Length / 1024.0:F1} KB" : $"{sizeMb:F1} MB";
                }
            }
        }
        catch { }

        var info = new DatabaseInfoDto
        {
            Type = dbType,
            IsConnected = isConnected,
            UserCount = isConnected ? await _context.Users.CountAsync() : 0,
            MessageCount = isConnected ? await _context.Messages.CountAsync() : 0,
            DatabaseSize = dbSize
        };

        return Ok(ApiResponse<DatabaseInfoDto>.Ok(info));
    }

    [HttpPost("database/config")]
    public async Task<ActionResult<ApiResponse<bool>>> SaveDatabaseConfig([FromBody] DatabaseConfigDto dto)
    {
        try
        {
            var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!System.IO.File.Exists(appSettingsPath))
            {
                return BadRequest(ApiResponse<bool>.Fail("appsettings.json bulunamadi"));
            }

            var json = await System.IO.File.ReadAllTextAsync(appSettingsPath);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = new Dictionary<string, object>();

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name == "Database")
                {
                    root["Database"] = new Dictionary<string, string> { ["Type"] = dto.Type };
                }
                else if (prop.Name == "ConnectionStrings")
                {
                    var connStrings = new Dictionary<string, string>();
                    foreach (var cs in prop.Value.EnumerateObject())
                    {
                        connStrings[cs.Name] = cs.Value.GetString() ?? "";
                    }
                    connStrings[dto.Type] = dto.ConnectionString;
                    root["ConnectionStrings"] = connStrings;
                }
                else
                {
                    root[prop.Name] = System.Text.Json.JsonSerializer.Deserialize<object>(prop.Value.GetRawText())!;
                }
            }

            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var newJson = System.Text.Json.JsonSerializer.Serialize(root, options);
            await System.IO.File.WriteAllTextAsync(appSettingsPath, newJson);

            await LogAction("SaveDatabaseConfig", "System", null, $"Database config changed to {dto.Type}");

            return Ok(ApiResponse<bool>.Ok(true, "Veritabani ayarlari kaydedildi. Sunucuyu yeniden baslatin."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save database config");
            return BadRequest(ApiResponse<bool>.Fail($"Ayarlar kaydedilemedi: {ex.Message}"));
        }
    }

    [HttpGet("database/test")]
    public async Task<ActionResult<ApiResponse<bool>>> TestDatabaseConnection()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            return Ok(canConnect
                ? ApiResponse<bool>.Ok(true, "Baglanti basarili")
                : ApiResponse<bool>.Fail("Veritabanina baglanamadi"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<bool>.Fail($"Baglanti hatasi: {ex.Message}"));
        }
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<ApiResponse<PagedResponse<AuditLogDto>>>> GetAuditLogs(
        [FromQuery] PaginationParams pagination)
    {
        var query = _context.AuditLogs
            .OrderByDescending(a => a.Timestamp);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                Timestamp = a.Timestamp,
                UserId = a.UserId,
                Username = a.Username,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Details = a.NewValues,
                IpAddress = a.IpAddress
            })
            .ToListAsync();

        var response = new PagedResponse<AuditLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };

        return Ok(ApiResponse<PagedResponse<AuditLogDto>>.Ok(response));
    }

    [HttpPost("notifications/send")]
    public async Task<ActionResult<ApiResponse<bool>>> SendNotification([FromBody] SendNotificationDto dto)
    {
        try
        {
            if (dto.TargetAll)
            {
                // Tüm bağlı kullanıcılara gönder
                await _hubContext.Clients.All.ReceiveNotification(dto.Title, dto.Message);
            }
            else if (dto.UserIds != null && dto.UserIds.Count > 0)
            {
                // Belirli kullanıcılara gönder
                foreach (var userId in dto.UserIds)
                {
                    await _hubContext.Clients.Group(SignalRConstants.Groups.User(userId))
                        .ReceiveNotification(dto.Title, dto.Message);
                }
            }

            await LogAction("SendNotification", "Notification", null,
                $"To: {(dto.TargetAll ? "All" : string.Join(",", dto.UserIds ?? []))} Message: {dto.Title}");

            return Ok(ApiResponse<bool>.Ok(true, "Bildirim gonderildi"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.Fail($"Bildirim gonderilemedi: {ex.Message}"));
        }
    }

    [HttpGet("config/{section}")]
    public ActionResult<ApiResponse<object>> GetConfig(string section)
    {
        var configSection = _configuration.GetSection(section);
        if (!configSection.Exists())
        {
            return NotFound(ApiResponse<object>.Fail("Yapilandirma bolumu bulunamadi"));
        }

        var dict = new Dictionary<string, string?>();
        foreach (var child in configSection.GetChildren())
        {
            if (child.GetChildren().Any())
            {
                foreach (var grandChild in child.GetChildren())
                {
                    dict[$"{child.Key}:{grandChild.Key}"] = grandChild.Value;
                }
            }
            else
            {
                dict[child.Key] = child.Value;
            }
        }

        return Ok(ApiResponse<object>.Ok(dict));
    }

    [HttpPost("config/{section}")]
    public async Task<ActionResult<ApiResponse<bool>>> SaveConfig(string section, [FromBody] Dictionary<string, object> config)
    {
        try
        {
            var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!System.IO.File.Exists(appSettingsPath))
            {
                return BadRequest(ApiResponse<bool>.Fail("appsettings.json bulunamadi"));
            }

            var json = await System.IO.File.ReadAllTextAsync(appSettingsPath);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = new Dictionary<string, object>();

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name == section)
                {
                    root[section] = config;
                }
                else
                {
                    root[prop.Name] = System.Text.Json.JsonSerializer.Deserialize<object>(prop.Value.GetRawText())!;
                }
            }

            if (!root.ContainsKey(section))
            {
                root[section] = config;
            }

            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var newJson = System.Text.Json.JsonSerializer.Serialize(root, options);
            await System.IO.File.WriteAllTextAsync(appSettingsPath, newJson);

            await LogAction("SaveConfig", "System", null, $"Config section '{section}' updated");

            return Ok(ApiResponse<bool>.Ok(true, "Yapilandirma kaydedildi. Degisikliklerin aktif olmasi icin sunucuyu yeniden baslatin."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.Fail($"Yapilandirma kaydedilemedi: {ex.Message}"));
        }
    }

    [HttpPost("users/{userId}/password")]
    public async Task<ActionResult<ApiResponse<bool>>> SetUserPassword(Guid userId, [FromBody] SetPasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<bool>.Fail("Kullanici bulunamadi"));
        }

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
        {
            return BadRequest(ApiResponse<bool>.Fail("Sifre en az 6 karakter olmalidir"));
        }

        user.PasswordHash = AuthService.HashPassword(dto.Password);
        await _context.SaveChangesAsync();

        await LogAction("SetUserPassword", "User", userId, $"Password set for: {user.Username}");

        return Ok(ApiResponse<bool>.Ok(true, "Sifre belirlendi"));
    }

    private async Task LogAction(string action, string entityType, Guid? entityId, string? details)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        Guid.TryParse(userIdClaim, out var userId);

        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            UserId = userId != Guid.Empty ? userId : null,
            Username = User.Identity?.Name ?? "system",
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            NewValues = details,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
