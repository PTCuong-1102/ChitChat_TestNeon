using Microsoft.EntityFrameworkCore;
using ChitChatApp.Core.Contracts.Repositories;
using ChitChatApp.Core.Domain.Entities;
using ChitChatApp.Core.Infrastructure.Persistence;

namespace ChitChatApp.Core.Infrastructure.Repositories
{
    /// <summary>
    /// Implementation of message repository
    /// </summary>
    public class MessageRepository : IMessageRepository
    {
        private readonly ApplicationDbContext _context;

        public MessageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Message?> GetMessageByIdAsync(long messageId)
        {
            return await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Room)
                .Include(m => m.ReplyTo)
                    .ThenInclude(rm => rm!.Sender)
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }

        public async Task<IEnumerable<Message>> GetRoomMessagesAsync(Guid roomId, int skip = 0, int take = 50)
        {
            return await _context.Messages
                .Where(m => m.RoomId == roomId)
                .Include(m => m.Sender)
                .Include(m => m.ReplyTo)
                    .ThenInclude(rm => rm!.Sender)
                .Include(m => m.Attachments)
                .OrderByDescending(m => m.SentAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<Message> CreateMessageAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<Message> UpdateMessageAsync(Message message)
        {
            message.EditedAt = DateTime.UtcNow;
            _context.Messages.Update(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<bool> DeleteMessageAsync(long messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null) return false;

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Message>> GetMessagesByUserAsync(Guid userId, int skip = 0, int take = 50)
        {
            return await _context.Messages
                .Where(m => m.SenderId == userId)
                .Include(m => m.Room)
                .OrderByDescending(m => m.SentAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetUnreadMessageCountAsync(Guid userId, Guid roomId)
        {
            return await _context.Messages
                .Where(m => m.RoomId == roomId && m.SenderId != userId)
                .Where(m => !_context.MessageStatuses
                    .Any(ms => ms.MessageId == m.Id && ms.UserId == userId && ms.IsRead))
                .CountAsync();
        }

        public async Task<IEnumerable<Message>> SearchMessagesInRoomAsync(Guid roomId, string searchTerm)
        {
            return await _context.Messages
                .Where(m => m.RoomId == roomId && m.Content.Contains(searchTerm))
                .Include(m => m.Sender)
                .OrderByDescending(m => m.SentAt)
                .Take(50)
                .ToListAsync();
        }
    }
}