using ChitChatApp.Core.Domain.Entities;

namespace ChitChatApp.Core.Contracts.Repositories
{ 
    /// <summary>
    /// Repository interface for managing contacts
    /// </summary>
    public interface IContactRepository
    {
        Task<IEnumerable<UserContact>> GetUserContactsAsync(Guid userId);
        Task<UserContact?> GetContactAsync(Guid userId, Guid contactId);
        Task<UserContact> AddContactAsync(Guid userId, Guid contactId);
        Task<bool> RemoveContactAsync(Guid userId, Guid contactId);
        Task<bool> IsContactAsync(Guid userId, Guid contactId);
        Task<IEnumerable<User>> GetMutualContactsAsync(Guid user1Id, Guid user2Id);
    }
}
