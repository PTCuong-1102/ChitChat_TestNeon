using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ChitChatApp.Core.Contracts.Repositories;
using ChitChatApp.Api.Models.DTOs;

namespace ChitChatApp.Api.Controllers
{
    /// <summary>
    /// Controller for managing bots and bot interactions
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BotsController : ControllerBase
    {
        private readonly IBotRepository _botRepository;
        private readonly ILogger<BotsController> _logger;

        public BotsController(IBotRepository botRepository, ILogger<BotsController> logger)
        {
            _botRepository = botRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get all active bots
        /// GET /api/bots
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BotDto>>> GetActiveBots()
        {
            var bots = await _botRepository.GetActiveBotsAsync();
            var botDtos = bots.Select(b => new BotDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt
            }).ToList();

            return Ok(botDtos);
        }

        /// <summary>
        /// Chat with a bot
        /// POST /api/bots/chat
        /// </summary>
        [HttpPost("chat")]
        public async Task<ActionResult<BotChatDto>> ChatWithBot([FromBody] BotChatRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var bot = await _botRepository.GetBotByIdAsync(request.BotId);
                if (bot == null) return NotFound("Bot not found");

                if (!bot.IsActive) return BadRequest("Bot is not active");

                // Simple echo bot implementation
                string botResponse = GenerateBotResponse(bot.Name, request.Message);

                var botChat = new Core.Domain.Entities.BotChat
                {
                    BotId = request.BotId,
                    UserId = userId.Value,
                    UserMessage = request.Message.Trim(),
                    BotResponse = botResponse,
                    CreatedAt = DateTime.UtcNow
                };

                var savedBotChat = await _botRepository.CreateBotChatAsync(botChat);

                return Ok(new BotChatDto
                {
                    Id = savedBotChat.Id,
                    UserMessage = savedBotChat.UserMessage,
                    BotResponse = savedBotChat.BotResponse,
                    CreatedAt = savedBotChat.CreatedAt,
                    Bot = new BotDto
                    {
                        Id = bot.Id,
                        Name = bot.Name,
                        Description = bot.Description,
                        IsActive = bot.IsActive,
                        CreatedAt = bot.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error chatting with bot {BotId} for user {UserId}", request.BotId, userId);
                return StatusCode(500, new { message = "Failed to process bot chat" });
            }
        }

        /// <summary>
        /// Get chat history with a specific bot
        /// GET /api/bots/{id}/history
        /// </summary>
        [HttpGet("{id}/history")]
        public async Task<ActionResult<IEnumerable<BotChatDto>>> GetBotChatHistory(
            Guid id, 
            [FromQuery] int skip = 0, 
            [FromQuery] int take = 50)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var chatHistory = await _botRepository.GetBotChatHistoryAsync(userId.Value, id, skip, take);
            var chatDtos = chatHistory.Select(bc => new BotChatDto
            {
                Id = bc.Id,
                UserMessage = bc.UserMessage,
                BotResponse = bc.BotResponse,
                CreatedAt = bc.CreatedAt,
                Bot = new BotDto
                {
                    Id = bc.Bot.Id,
                    Name = bc.Bot.Name,
                    Description = bc.Bot.Description,
                    IsActive = bc.Bot.IsActive,
                    CreatedAt = bc.Bot.CreatedAt
                }
            }).ToList();

            return Ok(chatDtos);
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private static string GenerateBotResponse(string botName, string userMessage)
        {
            // Simple bot response logic - you can extend this with more sophisticated AI
            return botName.ToLower() switch
            {
                "echo bot" => $"You said: {userMessage}",
                "welcome bot" => $"Welcome! You said '{userMessage}'. How can I help you today?",
                _ => $"Hi! I'm {botName}. You said: {userMessage}"
            };
        }
    }
}