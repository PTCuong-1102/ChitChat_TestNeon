using ChitChatApp.Core.Domain.Entities;

namespace ChitChatApp.Core.Contracts.Repositories
{
    /// <summary>
    /// Repository interface for managing messages
    /// </summary>
    public interface IMessageRepository
    {
        Task<Message?> GetMessageByIdAsync(long messageId);
        Task<IEnumerable<Message>> GetRoomMessagesAsync(Guid roomId, int skip = 0, int take = 50);
        Task<Message> CreateMessageAsync(Message message);
        Task<Message> UpdateMessageAsync(Message message);
        Task<bool> DeleteMessageAsync(long messageId);
        Task<IEnumerable<Message>> GetMessagesByUserAsync(Guid userId, int skip = 0, int take = 50);
        Task<int> GetUnreadMessageCountAsync(Guid userId, Guid roomId);
        Task<IEnumerable<Message>> SearchMessagesInRoomAsync(Guid roomId, string searchTerm);
    }
}
