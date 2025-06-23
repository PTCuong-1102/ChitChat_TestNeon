// BotChat.cs - Represents conversations between users and bots
using System.ComponentModel.DataAnnotations;

namespace ChitChatApp.Core.Domain.Entities
{
    public class BotChat
    {
        public long Id { get; set; } // Primary key, auto-incrementing long

        public Guid BotId { get; set; } // Which bot this conversation is with

        public Guid UserId { get; set; } // Which user is chatting with the bot

        [Required]
        public string UserMessage { get; set; } = string.Empty; // What the user said

        [Required]
        public string BotResponse { get; set; } = string.Empty; // How the bot responded

        public DateTime CreatedAt { get; set; } // When this exchange happened

        // Navigation properties
        public virtual Bot Bot { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
