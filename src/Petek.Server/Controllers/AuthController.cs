using Petek.Server.Services;
using Petek.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Petek.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Windows kimlik bilgileri ile giriş yap
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> Login([FromBody] WindowsLoginDto dto)
    {
        var result = await _authService.AuthenticateWithWindowsAsync(dto.Username, dto.Domain);

        if (!result.Success)
        {
            return Unauthorized(ApiResponse<AuthResultDto>.Fail(result.ErrorMessage ?? "Giriş başarısız"));
        }

        return Ok(ApiResponse<AuthResultDto>.Ok(result));
    }

    /// <summary>
    /// Kullanıcı adı ve şifre ile giriş yap
    /// </summary>
    [HttpPost("login/credentials")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> LoginWithCredentials([FromBody] CredentialsLoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(ApiResponse<AuthResultDto>.Fail("Kullanıcı adı ve şifre zorunludur"));
        }

        var result = await _authService.AuthenticateWithCredentialsAsync(dto.Username, dto.Password);

        if (!result.Success)
        {
            return Unauthorized(ApiResponse<AuthResultDto>.Fail(result.ErrorMessage ?? "Giriş başarısız"));
        }

        return Ok(ApiResponse<AuthResultDto>.Ok(result));
    }

    /// <summary>
    /// Windows Negotiate kimlik doğrulaması ile giriş
    /// </summary>
    [HttpPost("login/windows")]
    [Authorize(AuthenticationSchemes = "Negotiate")]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> LoginWithWindows()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized(ApiResponse<AuthResultDto>.Fail("Windows kimlik bilgileri alınamadı"));
        }

        var parts = username.Split('\\');
        var domain = parts.Length > 1 ? parts[0] : null;
        var user = parts.Length > 1 ? parts[1] : parts[0];

        var result = await _authService.AuthenticateWithWindowsAsync(user, domain);

        if (!result.Success)
        {
            return Unauthorized(ApiResponse<AuthResultDto>.Fail(result.ErrorMessage ?? "Giriş başarısız"));
        }

        return Ok(ApiResponse<AuthResultDto>.Ok(result));
    }

    /// <summary>
    /// Token yenile
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto.RefreshToken);

        if (!result.Success)
        {
            return Unauthorized(ApiResponse<AuthResultDto>.Fail(result.ErrorMessage ?? "Token yenilenemedu"));
        }

        return Ok(ApiResponse<AuthResultDto>.Ok(result));
    }

    /// <summary>
    /// Çıkış yap (oturumu sonlandır)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> Logout()
    {
        var sessionIdClaim = User.FindFirst("session_id")?.Value;
        if (Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            await _authService.RevokeSessionAsync(sessionId);
        }

        return Ok(ApiResponse<bool>.Ok(true, "Çıkış yapıldı"));
    }

    /// <summary>
    /// Kullanıcının aktif oturumlarını getir
    /// </summary>
    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<SessionDto>>>> GetSessions()
    {
        var userId = GetUserId();
        var sessions = await _authService.GetUserSessionsAsync(userId);
        return Ok(ApiResponse<List<SessionDto>>.Ok(sessions));
    }

    /// <summary>
    /// Belirli bir oturumu sonlandır
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> TerminateSession(Guid sessionId)
    {
        var result = await _authService.RevokeSessionAsync(sessionId);
        if (!result)
        {
            return NotFound(ApiResponse<bool>.Fail("Oturum bulunamadı"));
        }
        return Ok(ApiResponse<bool>.Ok(true, "Oturum sonlandırıldı"));
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
                          ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
