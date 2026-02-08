using Petek.Server.Data;
using Petek.Server.Data.Entities;
using Petek.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Petek.Server.Services;

public class ContactService : IContactService
{
    private readonly PetekDbContext _context;
    private readonly ILogger<ContactService> _logger;

    public ContactService(PetekDbContext context, ILogger<ContactService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ContactDto>> GetContactsAsync(Guid userId)
    {
        return await _context.Contacts
            .Include(c => c.ContactUser)
            .Include(c => c.Folder)
            .Where(c => c.OwnerId == userId)
            .OrderBy(c => c.ContactUser.DisplayName)
            .Select(c => new ContactDto
            {
                UserId = c.ContactUserId,
                DisplayName = c.ContactUser.DisplayName,
                Email = c.ContactUser.Email,
                Department = c.ContactUser.Department,
                Title = c.ContactUser.Title,
                AvatarUrl = c.ContactUser.AvatarUrl,
                Status = c.ContactUser.Status,
                IsFavorite = c.IsFavorite,
                FolderName = c.Folder != null ? c.Folder.Name : null,
                LastContactedAt = c.LastContactedAt
            })
            .ToListAsync();
    }

    public async Task<List<ContactFolderDto>> GetFoldersAsync(Guid userId)
    {
        return await _context.ContactFolders
            .Where(f => f.OwnerId == userId)
            .OrderBy(f => f.Order)
            .Select(f => new ContactFolderDto
            {
                Id = f.Id,
                Name = f.Name,
                ContactCount = f.Contacts.Count,
                Order = f.Order
            })
            .ToListAsync();
    }

    public async Task<ContactDto?> AddContactAsync(Guid userId, AddContactDto dto)
    {
        // Zaten kişi listesinde mi kontrol et
        var existing = await _context.Contacts
            .FirstOrDefaultAsync(c => c.OwnerId == userId && c.ContactUserId == dto.UserId);

        if (existing != null) return null;

        // Kullanıcı var mı kontrol et
        var contactUser = await _context.Users.FindAsync(dto.UserId);
        if (contactUser == null) return null;

        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            ContactUserId = dto.UserId,
            FolderId = dto.FolderId,
            IsFavorite = dto.IsFavorite,
            AddedAt = DateTime.UtcNow
        };

        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        return new ContactDto
        {
            UserId = contactUser.Id,
            DisplayName = contactUser.DisplayName,
            Email = contactUser.Email,
            Department = contactUser.Department,
            Title = contactUser.Title,
            AvatarUrl = contactUser.AvatarUrl,
            Status = contactUser.Status,
            IsFavorite = contact.IsFavorite
        };
    }

    public async Task<bool> RemoveContactAsync(Guid userId, Guid contactUserId)
    {
        var contact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.OwnerId == userId && c.ContactUserId == contactUserId);

        if (contact == null) return false;

        _context.Contacts.Remove(contact);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ContactFolderDto?> CreateFolderAsync(Guid userId, string name)
    {
        var maxOrder = await _context.ContactFolders
            .Where(f => f.OwnerId == userId)
            .MaxAsync(f => (int?)f.Order) ?? 0;

        var folder = new ContactFolder
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            Name = name,
            Order = maxOrder + 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.ContactFolders.Add(folder);
        await _context.SaveChangesAsync();

        return new ContactFolderDto
        {
            Id = folder.Id,
            Name = folder.Name,
            ContactCount = 0,
            Order = folder.Order
        };
    }

    public async Task<bool> DeleteFolderAsync(Guid userId, Guid folderId)
    {
        var folder = await _context.ContactFolders
            .FirstOrDefaultAsync(f => f.Id == folderId && f.OwnerId == userId);

        if (folder == null) return false;

        // Klasördeki kişileri klasörsüz yap
        var contacts = await _context.Contacts
            .Where(c => c.FolderId == folderId)
            .ToListAsync();

        foreach (var contact in contacts)
        {
            contact.FolderId = null;
        }

        _context.ContactFolders.Remove(folder);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetFavoriteAsync(Guid userId, Guid contactUserId, bool isFavorite)
    {
        var contact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.OwnerId == userId && c.ContactUserId == contactUserId);

        if (contact == null) return false;

        contact.IsFavorite = isFavorite;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MoveToFolderAsync(Guid userId, Guid contactUserId, Guid? folderId)
    {
        var contact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.OwnerId == userId && c.ContactUserId == contactUserId);

        if (contact == null) return false;

        if (folderId.HasValue)
        {
            var folder = await _context.ContactFolders
                .FirstOrDefaultAsync(f => f.Id == folderId.Value && f.OwnerId == userId);
            if (folder == null) return false;
        }

        contact.FolderId = folderId;
        await _context.SaveChangesAsync();
        return true;
    }
}
