// Bot.cs - Represents chat bots in the system
using System.ComponentModel.DataAnnotations;

namespace ChitChatApp.Core.Domain.Entities
{
    public class Bot
    {
        public Guid Id { get; set; } // Primary key, UUID

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; } // What this bot does

        public bool IsActive { get; set; } = true; // Whether the bot is currently active

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<BotChat> BotChats { get; set; } = new List<BotChat>();
    }
}
