namespace Petek.Server.Data.Entities;

/// <summary>
/// Kişi entity (kullanıcının kişi listesi)
/// </summary>
public class Contact
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public Guid ContactUserId { get; set; }
    public Guid? FolderId { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastContactedAt { get; set; }

    // Navigation properties
    public virtual User Owner { get; set; } = null!;
    public virtual User ContactUser { get; set; } = null!;
    public virtual ContactFolder? Folder { get; set; }
}

/// <summary>
/// Kişi klasörü entity
/// </summary>
public class ContactFolder
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User Owner { get; set; } = null!;
    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}
