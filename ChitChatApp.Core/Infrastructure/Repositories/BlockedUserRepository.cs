using Microsoft.EntityFrameworkCore;
using ChitChatApp.Core.Contracts.Repositories;
using ChitChatApp.Core.Domain.Entities;
using ChitChatApp.Core.Infrastructure.Persistence;

namespace ChitChatApp.Core.Infrastructure.Repositories
{
    /// <summary>
    /// Implementation of blocked user repository
    /// </summary>
    public class BlockedUserRepository : IBlockedUserRepository
    {
        private readonly ApplicationDbContext _context;

        public BlockedUserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BlockedUser>> GetBlockedUsersAsync(Guid userId)
        {
            return await _context.BlockedUsers
                .Where(bu => bu.UserId == userId)
                .Include(bu => bu.BlockedUserEntity)
                .OrderBy(bu => bu.BlockedUserEntity.FullName)
                .ToListAsync();
        }

        public async Task<BlockedUser?> GetBlockedUserAsync(Guid userId, Guid blockedUserId)
        {
            return await _context.BlockedUsers
                .Include(bu => bu.BlockedUserEntity)
                .FirstOrDefaultAsync(bu => bu.UserId == userId && bu.BlockedUserId == blockedUserId);
        }

        public async Task<BlockedUser> BlockUserAsync(Guid userId, Guid userToBlockId)
        {
            var blockedUser = new BlockedUser
            {
                UserId = userId,
                BlockedUserId = userToBlockId,
                BlockedAt = DateTime.UtcNow
            };

            _context.BlockedUsers.Add(blockedUser);
            await _context.SaveChangesAsync();
            return blockedUser;
        }

        public async Task<bool> UnblockUserAsync(Guid userId, Guid userToUnblockId)
        {
            var blockedUser = await _context.BlockedUsers
                .FirstOrDefaultAsync(bu => bu.UserId == userId && bu.BlockedUserId == userToUnblockId);

            if (blockedUser == null) return false;

            _context.BlockedUsers.Remove(blockedUser);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsUserBlockedAsync(Guid userId, Guid potentiallyBlockedUserId)
        {
            return await _context.BlockedUsers
                .AnyAsync(bu => bu.UserId == userId && bu.BlockedUserId == potentiallyBlockedUserId);
        }
    }
}