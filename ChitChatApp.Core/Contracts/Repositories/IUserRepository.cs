using ChitChatApp.Core.Domain.Entities;
namespace ChitChatApp.Core.Contracts.Repositories {
    /// <summary>
    /// Repository interface for managing users
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByUserNameAsync(string userName);
        Task<IEnumerable<User>> GetUsersAsync(int skip = 0, int take = 50);
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(Guid userId);
        Task<IEnumerable<User>> GetUserContactsAsync(Guid userId);
        Task<IEnumerable<User>> GetOnlineUsersAsync();
    }
}

