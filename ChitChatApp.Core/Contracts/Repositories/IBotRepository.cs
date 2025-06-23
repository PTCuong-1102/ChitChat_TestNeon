using ChitChatApp.Core.Domain.Entities;

namespace ChitChatApp.Core.Contracts.Repositories
{   
    /// <summary>
    /// Repository interface for managing bots
    /// </summary>
    public interface IBotRepository
    {
        Task<IEnumerable<Bot>> GetActiveBotsAsync();
        Task<Bot?> GetBotByIdAsync(Guid botId);
        Task<Bot?> GetBotByNameAsync(string name);
        Task<Bot> CreateBotAsync(Bot bot);
        Task<Bot> UpdateBotAsync(Bot bot);
        Task<IEnumerable<BotChat>> GetBotChatHistoryAsync(Guid userId, Guid botId, int skip = 0, int take = 50);
        Task<BotChat> CreateBotChatAsync(BotChat botChat);
    }
}