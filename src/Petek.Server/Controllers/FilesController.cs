using Petek.Server.Services;
using Petek.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Petek.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IFileService fileService, ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    /// <summary>
    /// Dosya yükle
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(104857600)] // 100 MB
    public async Task<ActionResult<ApiResponse<FileUploadResultDto>>> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<FileUploadResultDto>.Fail("Dosya seçilmedi"));
        }

        var userId = GetUserId();
        using var stream = file.OpenReadStream();
        var result = await _fileService.UploadFileAsync(userId, stream, file.FileName, file.ContentType);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<FileUploadResultDto>.Fail(result.ErrorMessage ?? "Yükleme başarısız"));
        }

        return Ok(ApiResponse<FileUploadResultDto>.Ok(result, "Dosya yüklendi"));
    }

    /// <summary>
    /// Dosya indir
    /// </summary>
    [HttpGet("{fileId}")]
    public async Task<IActionResult> Download(Guid fileId)
    {
        var userId = GetUserId();
        var stream = await _fileService.DownloadFileAsync(fileId, userId);

        if (stream == null)
        {
            return NotFound();
        }

        return File(stream, "application/octet-stream");
    }

    /// <summary>
    /// Dosya sil
    /// </summary>
    [HttpDelete("{fileId}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid fileId)
    {
        var userId = GetUserId();
        var result = await _fileService.DeleteFileAsync(fileId, userId);

        if (!result)
        {
            return NotFound(ApiResponse<bool>.Fail("Dosya bulunamadı veya silme yetkiniz yok"));
        }

        return Ok(ApiResponse<bool>.Ok(true, "Dosya silindi"));
    }

    /// <summary>
    /// Dosya politikasını getir
    /// </summary>
    [HttpGet("policy")]
    public async Task<ActionResult<ApiResponse<FilePolicyDto>>> GetPolicy()
    {
        var policy = await _fileService.GetFilePolicyAsync();
        return Ok(ApiResponse<FilePolicyDto>.Ok(policy));
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
                          ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
