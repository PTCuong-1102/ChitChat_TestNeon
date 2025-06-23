using Microsoft.EntityFrameworkCore;
using ChitChatApp.Core.Contracts.Repositories;
using ChitChatApp.Core.Domain.Entities;
using ChitChatApp.Core.Infrastructure.Persistence;

namespace ChitChatApp.Core.Infrastructure.Repositories
{
    /// <summary>
    /// Implementation of room repository with Entity Framework Core
    /// This class handles all database operations related to chat rooms
    /// </summary>
    public class RoomRepository : IRoomRepository
    {
        private readonly ApplicationDbContext _context;

        public RoomRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ChatRoom>> GetRoomsForUserAsync(Guid userId)
        {
            // Get room IDs first
            var roomIds = await _context.RoomParticipants
                .Where(rp => rp.UserId == userId)
                .Select(rp => rp.RoomId)
                .ToListAsync();

            // Then get rooms with participants (without complex nested includes)
            var rooms = await _context.ChatRooms
                .Where(r => roomIds.Contains(r.Id))
                .Include(r => r.Participants)
                    .ThenInclude(p => p.User)
                .ToListAsync();

            // Load latest message for each room separately
            foreach (var room in rooms)
            {
                var latestMessage = await _context.Messages
                    .Where(m => m.RoomId == room.Id)
                    .Include(m => m.Sender)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();
                
                if (latestMessage != null)
                {
                    room.Messages = new List<Message> { latestMessage };
                }
            }

            // Sort by latest message time or creation time
            return rooms.OrderByDescending(r => 
                r.Messages?.FirstOrDefault()?.SentAt ?? r.CreatedAt);
        }

        public async Task<ChatRoom?> GetRoomByIdAsync(Guid roomId)
        {
            return await _context.ChatRooms
                .Include(r => r.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(r => r.Id == roomId);
        }

        public async Task<ChatRoom?> GetRoomWithParticipantsAsync(Guid roomId)
        {
            return await _context.ChatRooms
                .Include(r => r.Participants)
                    .ThenInclude(p => p.User)
                .Include(r => r.Messages.OrderByDescending(m => m.SentAt).Take(50))
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(r => r.Id == roomId);
        }

        public async Task<ChatRoom> CreateRoomAsync(ChatRoom room)
        {
            _context.ChatRooms.Add(room);
            await _context.SaveChangesAsync();
            return room;
        }

        public async Task<ChatRoom> UpdateRoomAsync(ChatRoom room)
        {
            room.UpdatedAt = DateTime.UtcNow;
            _context.ChatRooms.Update(room);
            await _context.SaveChangesAsync();
            return room;
        }

        public async Task<bool> DeleteRoomAsync(Guid roomId)
        {
            var room = await _context.ChatRooms.FindAsync(roomId);
            if (room == null) return false;

            _context.ChatRooms.Remove(room);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddUserToRoomAsync(Guid roomId, Guid userId, bool isAdmin = false)
        {
            // Check if user is already in the room
            var existingParticipant = await _context.RoomParticipants
                .FirstOrDefaultAsync(rp => rp.RoomId == roomId && rp.UserId == userId);

            if (existingParticipant != null) return false;

            var participant = new RoomParticipant
            {
                RoomId = roomId,
                UserId = userId,
                IsAdmin = isAdmin,
                JoinedAt = DateTime.UtcNow
            };

            _context.RoomParticipants.Add(participant);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveUserFromRoomAsync(Guid roomId, Guid userId)
        {
            var participant = await _context.RoomParticipants
                .FirstOrDefaultAsync(rp => rp.RoomId == roomId && rp.UserId == userId);

            if (participant == null) return false;

            _context.RoomParticipants.Remove(participant);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsUserInRoomAsync(Guid roomId, Guid userId)
        {
            return await _context.RoomParticipants
                .AnyAsync(rp => rp.RoomId == roomId && rp.UserId == userId);
        }

        public async Task<IEnumerable<ChatRoom>> SearchRoomsAsync(string searchTerm)
        {
            return await _context.ChatRooms
                .Where(r => !r.IsPrivate && 
                           (r.Name.Contains(searchTerm) || 
                            (r.Description != null && r.Description.Contains(searchTerm))))
                .Include(r => r.Participants)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<ChatRoom?> GetOrCreatePrivateRoomAsync(Guid user1Id, Guid user2Id)
        {
            // Look for existing private room between these two users
            var existingRoom = await _context.RoomParticipants
                .Where(rp1 => rp1.UserId == user1Id)
                .Join(_context.RoomParticipants.Where(rp2 => rp2.UserId == user2Id),
                      rp1 => rp1.RoomId,
                      rp2 => rp2.RoomId,
                      (rp1, rp2) => rp1.Room)
                .Where(r => r.IsPrivate)
                .Include(r => r.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync();

            if (existingRoom != null)
                return existingRoom;

            // Create new private room
            var newRoom = new ChatRoom
            {
                Name = "Private Chat",
                IsPrivate = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ChatRooms.Add(newRoom);
            await _context.SaveChangesAsync();

            // Add both users as participants
            await AddUserToRoomAsync(newRoom.Id, user1Id);
            await AddUserToRoomAsync(newRoom.Id, user2Id);

            return await GetRoomWithParticipantsAsync(newRoom.Id);
        }
    }
}