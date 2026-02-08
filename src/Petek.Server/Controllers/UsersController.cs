using Petek.Server.Services;
using Petek.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Petek.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm kullanıcıları getir
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsers()
    {
        var users = await _userService.GetUsersAsync();
        return Ok(ApiResponse<List<UserDto>>.Ok(users));
    }

    /// <summary>
    /// Kullanıcı ara
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> SearchUsers([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return BadRequest(ApiResponse<List<UserDto>>.Fail("Arama sorgusu en az 2 karakter olmalıdır"));
        }

        var users = await _userService.SearchUsersAsync(q);
        return Ok(ApiResponse<List<UserDto>>.Ok(users));
    }

    /// <summary>
    /// Departmanları ve kullanıcıları getir
    /// </summary>
    [HttpGet("departments")]
    public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> GetDepartments()
    {
        var departments = await _userService.GetDepartmentsAsync();
        return Ok(ApiResponse<List<DepartmentDto>>.Ok(departments));
    }

    /// <summary>
    /// Belirli bir kullanıcıyı getir
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid userId)
    {
        var user = await _userService.GetUserAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("Kullanıcı bulunamadı"));
        }
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    /// <summary>
    /// Mevcut kullanıcının profilini getir
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
    {
        var userId = GetUserId();
        var user = await _userService.GetUserAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("Kullanıcı bulunamadı"));
        }
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    /// <summary>
    /// Mevcut kullanıcının profilini güncelle
    /// </summary>
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateProfile([FromBody] UpdateUserProfileDto dto)
    {
        var userId = GetUserId();
        var result = await _userService.UpdateProfileAsync(userId, dto);
        if (!result)
        {
            return NotFound(ApiResponse<bool>.Fail("Profil güncellenemedi"));
        }
        return Ok(ApiResponse<bool>.Ok(true, "Profil güncellendi"));
    }

    /// <summary>
    /// Kullanıcı durumunu güncelle
    /// </summary>
    [HttpPut("me/status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStatus([FromBody] UpdateUserStatusDto dto)
    {
        var userId = GetUserId();
        var result = await _userService.UpdateStatusAsync(userId, dto.Status);
        if (!result)
        {
            return NotFound(ApiResponse<bool>.Fail("Durum güncellenemedi"));
        }
        return Ok(ApiResponse<bool>.Ok(true, "Durum güncellendi"));
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
                          ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
