using Petek.Shared.Enums;

namespace Petek.Shared.DTOs;

/// <summary>
/// Kişi veri transfer nesnesi
/// </summary>
public class ContactDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; }
    public bool IsFavorite { get; set; }
    public string? FolderName { get; set; }
    public DateTime? LastContactedAt { get; set; }
}

/// <summary>
/// Kişi klasörü
/// </summary>
public class ContactFolderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ContactCount { get; set; }
    public int Order { get; set; }
}

/// <summary>
/// Kişi ekleme
/// </summary>
public class AddContactDto
{
    public Guid UserId { get; set; }
    public Guid? FolderId { get; set; }
    public bool IsFavorite { get; set; }
}

/// <summary>
/// Departman bilgisi
/// </summary>
public class DepartmentDto
{
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public List<ContactDto> Users { get; set; } = new();
}
