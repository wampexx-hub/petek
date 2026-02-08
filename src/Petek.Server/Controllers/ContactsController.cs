using Petek.Server.Services;
using Petek.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Petek.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactsController : ControllerBase
{
    private readonly IContactService _contactService;
    private readonly ILogger<ContactsController> _logger;

    public ContactsController(IContactService contactService, ILogger<ContactsController> logger)
    {
        _contactService = contactService;
        _logger = logger;
    }

    /// <summary>
    /// Kişi listesini getir
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ContactDto>>>> GetContacts()
    {
        var userId = GetUserId();
        var contacts = await _contactService.GetContactsAsync(userId);
        return Ok(ApiResponse<List<ContactDto>>.Ok(contacts));
    }

    /// <summary>
    /// Kişi klasörlerini getir
    /// </summary>
    [HttpGet("folders")]
    public async Task<ActionResult<ApiResponse<List<ContactFolderDto>>>> GetFolders()
    {
        var userId = GetUserId();
        var folders = await _contactService.GetFoldersAsync(userId);
        return Ok(ApiResponse<List<ContactFolderDto>>.Ok(folders));
    }

    /// <summary>
    /// Kişi ekle
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ContactDto>>> AddContact([FromBody] AddContactDto dto)
    {
        var userId = GetUserId();
        var contact = await _contactService.AddContactAsync(userId, dto);
        if (contact == null)
        {
            return BadRequest(ApiResponse<ContactDto>.Fail("Kişi eklenemedi veya zaten mevcut"));
        }
        return Ok(ApiResponse<ContactDto>.Ok(contact, "Kişi eklendi"));
    }

    /// <summary>
    /// Kişi sil
    /// </summary>
    [HttpDelete("{contactUserId}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveContact(Guid contactUserId)
    {
        var userId = GetUserId();
        var result = await _contactService.RemoveContactAsync(userId, contactUserId);
        if (!result)
        {
            return NotFound(ApiResponse<bool>.Fail("Kişi bulunamadı"));
        }
        return Ok(ApiResponse<bool>.Ok(true, "Kişi silindi"));
    }

    /// <summary>
    /// Yeni klasör oluştur
    /// </summary>
    [HttpPost("folders")]
    public async Task<ActionResult<ApiResponse<ContactFolderDto>>> CreateFolder([FromBody] string name)
    {
        var userId = GetUserId();
        var folder = await _contactService.CreateFolderAsync(userId, name);
        if (folder == null)
        {
            return BadRequest(ApiResponse<ContactFolderDto>.Fail("Klasör oluşturulamadı"));
        }
        return Ok(ApiResponse<ContactFolderDto>.Ok(folder, "Klasör oluşturuldu"));
    }

    /// <summary>
    /// Klasör sil
    /// </summary>
    [HttpDelete("folders/{folderId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteFolder(Guid folderId)
    {
        var userId = GetUserId();
        var result = await _contactService.DeleteFolderAsync(userId, folderId);
        if (!result)
        {
            return NotFound(ApiResponse<bool>.Fail("Klasör bulunamadı"));
        }
        return Ok(ApiResponse<bool>.Ok(true, "Klasör silindi"));
    }

    /// <summary>
    /// Kişiyi favorilere ekle/çıkar
    /// </summary>
    [HttpPut("{contactUserId}/favorite")]
    public async Task<ActionResult<ApiResponse<bool>>> SetFavorite(Guid contactUserId, [FromBody] bool isFavorite)
    {
        var userId = GetUserId();
        var result = await _contactService.SetFavoriteAsync(userId, contactUserId, isFavorite);
        if (!result)
        {
            return NotFound(ApiResponse<bool>.Fail("Kişi bulunamadı"));
        }
        return Ok(ApiResponse<bool>.Ok(true, isFavorite ? "Favorilere eklendi" : "Favorilerden çıkarıldı"));
    }

    /// <summary>
    /// Kişiyi klasöre taşı
    /// </summary>
    [HttpPut("{contactUserId}/folder")]
    public async Task<ActionResult<ApiResponse<bool>>> MoveToFolder(Guid contactUserId, [FromBody] Guid? folderId)
    {
        var userId = GetUserId();
        var result = await _contactService.MoveToFolderAsync(userId, contactUserId, folderId);
        if (!result)
        {
            return NotFound(ApiResponse<bool>.Fail("Kişi veya klasör bulunamadı"));
        }
        return Ok(ApiResponse<bool>.Ok(true, "Kişi taşındı"));
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
                          ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
