using Petek.Server.Data;
using Petek.Server.Data.Entities;
using Petek.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Petek.Server.Services;

public class FileService : IFileService
{
    private readonly PetekDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ISurveillanceService _surveillanceService;
    private readonly ILogger<FileService> _logger;
    private readonly string _storagePath;

    // Varsayılan dosya politikası
    private static readonly FilePolicyDto DefaultPolicy = new()
    {
        MaxFileSizeBytes = 100 * 1024 * 1024, // 100 MB
        AllowedExtensions = new List<string>
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".txt", ".rtf", ".csv", ".zip", ".rar", ".7z",
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg",
            ".mp3", ".mp4", ".avi", ".mov", ".wmv"
        },
        BlockedExtensions = new List<string>
        {
            ".exe", ".bat", ".cmd", ".com", ".msi", ".scr", ".vbs", ".js", ".ps1"
        },
        AllowImages = true,
        AllowDocuments = true,
        AllowArchives = true
    };

    public FileService(
        PetekDbContext context,
        IConfiguration configuration,
        ISurveillanceService surveillanceService,
        ILogger<FileService> logger)
    {
        _context = context;
        _configuration = configuration;
        _surveillanceService = surveillanceService;
        _logger = logger;
        _storagePath = configuration["FileStorage:Path"] ?? Path.Combine(AppContext.BaseDirectory, "uploads");

        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<FileUploadResultDto> UploadFileAsync(Guid userId, Stream fileStream, string fileName, string contentType)
    {
        var policy = await GetFilePolicyAsync();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        // Politika kontrolü
        if (policy.BlockedExtensions.Contains(extension))
        {
            return new FileUploadResultDto
            {
                Success = false,
                ErrorMessage = "Bu dosya türü engellenmiştir"
            };
        }

        if (policy.AllowedExtensions.Count > 0 && !policy.AllowedExtensions.Contains(extension))
        {
            return new FileUploadResultDto
            {
                Success = false,
                ErrorMessage = "Bu dosya türüne izin verilmemektedir"
            };
        }

        // Dosya boyutu kontrolü
        if (fileStream.Length > policy.MaxFileSizeBytes)
        {
            return new FileUploadResultDto
            {
                Success = false,
                ErrorMessage = $"Dosya boyutu maksimum {policy.MaxFileSizeBytes / (1024 * 1024)} MB olmalıdır"
            };
        }

        try
        {
            var fileId = Guid.NewGuid();
            var storageName = $"{fileId}{extension}";
            var storagePath = Path.Combine(_storagePath, DateTime.UtcNow.ToString("yyyy/MM/dd"));

            if (!Directory.Exists(storagePath))
            {
                Directory.CreateDirectory(storagePath);
            }

            var fullPath = Path.Combine(storagePath, storageName);

            // Checksum hesapla
            string checksum;
            using (var md5 = MD5.Create())
            {
                var hash = await md5.ComputeHashAsync(fileStream);
                checksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                fileStream.Position = 0;
            }

            // Dosyayı kaydet
            await using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fs);
            }

            var attachment = new FileAttachment
            {
                Id = fileId,
                FileName = fileName,
                FileExtension = extension,
                ContentType = contentType,
                FileSize = fileStream.Length,
                StoragePath = fullPath,
                Checksum = checksum,
                UploadedAt = DateTime.UtcNow,
                UploadedByUserId = userId
            };

            var result = new FileUploadResultDto
            {
                FileId = fileId,
                FileName = fileName,
                DownloadUrl = $"/api/files/{fileId}",
                FileSize = fileStream.Length,
                Success = true
            };

            // Thumbnail oluştur (görsel dosyalar için)
            if (IsImageFile(extension))
            {
                result.ThumbnailUrl = $"/api/files/{fileId}/thumbnail";
            }

            // Surveillance servisine bildir
            await _surveillanceService.SendFileEventAsync(new FileAttachmentDto
            {
                Id = fileId,
                FileName = fileName,
                FileExtension = extension,
                ContentType = contentType,
                FileSize = fileStream.Length,
                UploadedAt = attachment.UploadedAt
            }, userId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName}", fileName);
            return new FileUploadResultDto
            {
                Success = false,
                ErrorMessage = "Dosya yüklenirken bir hata oluştu"
            };
        }
    }

    public async Task<Stream?> DownloadFileAsync(Guid fileId, Guid userId)
    {
        var attachment = await _context.FileAttachments.FindAsync(fileId);
        if (attachment == null) return null;

        if (!File.Exists(attachment.StoragePath)) return null;

        return new FileStream(attachment.StoragePath, FileMode.Open, FileAccess.Read);
    }

    public async Task<bool> DeleteFileAsync(Guid fileId, Guid userId)
    {
        var attachment = await _context.FileAttachments.FindAsync(fileId);
        if (attachment == null) return false;

        // Sadece yükleyen kişi silebilir
        if (attachment.UploadedByUserId != userId) return false;

        try
        {
            if (File.Exists(attachment.StoragePath))
            {
                File.Delete(attachment.StoragePath);
            }

            _context.FileAttachments.Remove(attachment);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileId}", fileId);
            return false;
        }
    }

    public async Task<FilePolicyDto> GetFilePolicyAsync()
    {
        // Veritabanından özelleştirilmiş politikayı al
        var maxSizeSetting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == "file.maxSizeBytes");

        var policy = new FilePolicyDto
        {
            MaxFileSizeBytes = maxSizeSetting != null && long.TryParse(maxSizeSetting.Value, out var size)
                ? size
                : DefaultPolicy.MaxFileSizeBytes,
            AllowedExtensions = DefaultPolicy.AllowedExtensions,
            BlockedExtensions = DefaultPolicy.BlockedExtensions,
            AllowImages = DefaultPolicy.AllowImages,
            AllowDocuments = DefaultPolicy.AllowDocuments,
            AllowArchives = DefaultPolicy.AllowArchives
        };

        return policy;
    }

    private static bool IsImageFile(string extension)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg" };
        return imageExtensions.Contains(extension.ToLowerInvariant());
    }
}
