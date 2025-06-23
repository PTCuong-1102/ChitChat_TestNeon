using System.ComponentModel.DataAnnotations;

namespace ChitChatApp.Api.Models.DTOs{
    public class BotChatRequestDto
    {
        [Required]
        public Guid BotId { get; set; }
        
        [Required]
        public string Message { get; set; } = string.Empty;
    }
}