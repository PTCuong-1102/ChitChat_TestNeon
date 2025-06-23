using Microsoft.EntityFrameworkCore;
using ChitChatApp.Core.Contracts.Repositories;
using ChitChatApp.Core.Domain.Entities;
using ChitChatApp.Core.Infrastructure.Persistence;

namespace ChitChatApp.Core.Infrastructure.Repositories
{
    /// <summary>
    /// Implementation of bot repository
    /// </summary>
    public class BotRepository : IBotRepository
    {
        private readonly ApplicationDbContext _context;

        public BotRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Bot>> GetActiveBotsAsync()
        {
            return await _context.Bots
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<Bot?> GetBotByIdAsync(Guid botId)
        {
            return await _context.Bots.FindAsync(botId);
        }

        public async Task<Bot?> GetBotByNameAsync(string name)
        {
            return await _context.Bots
                .FirstOrDefaultAsync(b => b.Name.ToLower() == name.ToLower());
        }

        public async Task<Bot> CreateBotAsync(Bot bot)
        {
            _context.Bots.Add(bot);
            await _context.SaveChangesAsync();
            return bot;
        }

        public async Task<Bot> UpdateBotAsync(Bot bot)
        {
            bot.UpdatedAt = DateTime.UtcNow;
            _context.Bots.Update(bot);
            await _context.SaveChangesAsync();
            return bot;
        }

        public async Task<IEnumerable<BotChat>> GetBotChatHistoryAsync(Guid userId, Guid botId, int skip = 0, int take = 50)
        {
            return await _context.BotChats
                .Where(bc => bc.UserId == userId && bc.BotId == botId)
                .Include(bc => bc.Bot)
                .OrderByDescending(bc => bc.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<BotChat> CreateBotChatAsync(BotChat botChat)
        {
            _context.BotChats.Add(botChat);
            await _context.SaveChangesAsync();
            return botChat;
        }
    }
}