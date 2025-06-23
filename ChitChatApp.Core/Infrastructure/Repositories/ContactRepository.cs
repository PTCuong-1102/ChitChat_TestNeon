using Microsoft.EntityFrameworkCore;
using ChitChatApp.Core.Contracts.Repositories;
using ChitChatApp.Core.Domain.Entities;
using ChitChatApp.Core.Infrastructure.Persistence;

namespace ChitChatApp.Core.Infrastructure.Repositories
{
    /// <summary>
    /// Implementation of contact repository
    /// </summary>
    public class ContactRepository : IContactRepository
    {
        private readonly ApplicationDbContext _context;

        public ContactRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserContact>> GetUserContactsAsync(Guid userId)
        {
            return await _context.UserContacts
                .Where(uc => uc.UserId == userId)
                .Include(uc => uc.Contact)
                .OrderBy(uc => uc.Contact.FullName)
                .ToListAsync();
        }

        public async Task<UserContact?> GetContactAsync(Guid userId, Guid contactId)
        {
            return await _context.UserContacts
                .Include(uc => uc.Contact)
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.ContactId == contactId);
        }

        public async Task<UserContact> AddContactAsync(Guid userId, Guid contactId)
        {
            var contact = new UserContact
            {
                UserId = userId,
                ContactId = contactId,
                AddedAt = DateTime.UtcNow
            };

            _context.UserContacts.Add(contact);
            await _context.SaveChangesAsync();
            return contact;
        }

        public async Task<bool> RemoveContactAsync(Guid userId, Guid contactId)
        {
            var contact = await _context.UserContacts
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.ContactId == contactId);

            if (contact == null) return false;

            _context.UserContacts.Remove(contact);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsContactAsync(Guid userId, Guid contactId)
        {
            return await _context.UserContacts
                .AnyAsync(uc => uc.UserId == userId && uc.ContactId == contactId);
        }

        public async Task<IEnumerable<User>> GetMutualContactsAsync(Guid user1Id, Guid user2Id)
        {
            var user1Contacts = _context.UserContacts
                .Where(uc => uc.UserId == user1Id)
                .Select(uc => uc.ContactId);

            var user2Contacts = _context.UserContacts
                .Where(uc => uc.UserId == user2Id)
                .Select(uc => uc.ContactId);

            var mutualContactIds = user1Contacts.Intersect(user2Contacts);

            return await _context.Users
                .Where(u => mutualContactIds.Contains(u.Id))
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }
    }
}