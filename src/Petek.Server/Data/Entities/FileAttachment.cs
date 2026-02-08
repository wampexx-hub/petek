namespace Petek.Server.Data.Entities;

/// <summary>
/// Dosya eki entity
/// </summary>
public class FileAttachment
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public string? Checksum { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public Guid UploadedByUserId { get; set; }

    // Navigation properties
    public virtual Message Message { get; set; } = null!;
    public virtual User UploadedBy { get; set; } = null!;
}
