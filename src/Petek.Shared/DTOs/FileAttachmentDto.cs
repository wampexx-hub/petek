namespace Petek.Shared.DTOs;

/// <summary>
/// Dosya eki veri transfer nesnesi
/// </summary>
public class FileAttachmentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public DateTime UploadedAt { get; set; }
}

/// <summary>
/// Dosya yükleme sonucu
/// </summary>
public class FileUploadResultDto
{
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public long FileSize { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Dosya paylaşım politikası
/// </summary>
public class FilePolicyDto
{
    public long MaxFileSizeBytes { get; set; }
    public List<string> AllowedExtensions { get; set; } = new();
    public List<string> BlockedExtensions { get; set; } = new();
    public bool AllowImages { get; set; } = true;
    public bool AllowDocuments { get; set; } = true;
    public bool AllowArchives { get; set; } = true;
}
