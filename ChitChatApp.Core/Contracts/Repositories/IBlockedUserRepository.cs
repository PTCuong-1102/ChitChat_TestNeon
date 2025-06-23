using ChitChatApp.Core.Domain.Entities;

namespace ChitChatApp.Core.Contracts.Repositories
{
    /// <summary>
    /// Repository interface for managing blocked users
    /// </summary>
    public interface IBlockedUserRepository
    {
        Task<IEnumerable<BlockedUser>> GetBlockedUsersAsync(Guid userId);
        Task<BlockedUser?> GetBlockedUserAsync(Guid userId, Guid blockedUserId);
        Task<BlockedUser> BlockUserAsync(Guid userId, Guid userToBlockId);
        Task<bool> UnblockUserAsync(Guid userId, Guid userToUnblockId);
        Task<bool> IsUserBlockedAsync(Guid userId, Guid potentiallyBlockedUserId);
    }
}