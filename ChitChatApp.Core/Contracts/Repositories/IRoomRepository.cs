using ChitChatApp.Core.Domain.Entities;

namespace ChitChatApp.Core.Contracts.Repositories
{
    /// <summary>
    /// Repository interface for managing chat rooms
    /// This defines the contract for all room-related data operations
    /// </summary>
    public interface IRoomRepository
    {
        Task<IEnumerable<ChatRoom>> GetRoomsForUserAsync(Guid userId);
        Task<ChatRoom?> GetRoomByIdAsync(Guid roomId);
        Task<ChatRoom?> GetRoomWithParticipantsAsync(Guid roomId);
        Task<ChatRoom> CreateRoomAsync(ChatRoom room);
        Task<ChatRoom> UpdateRoomAsync(ChatRoom room);
        Task<bool> DeleteRoomAsync(Guid roomId);
        Task<bool> AddUserToRoomAsync(Guid roomId, Guid userId, bool isAdmin = false);
        Task<bool> RemoveUserFromRoomAsync(Guid roomId, Guid userId);
        Task<bool> IsUserInRoomAsync(Guid roomId, Guid userId);
        Task<IEnumerable<ChatRoom>> SearchRoomsAsync(string searchTerm);
        Task<ChatRoom?> GetOrCreatePrivateRoomAsync(Guid user1Id, Guid user2Id);
    }
}