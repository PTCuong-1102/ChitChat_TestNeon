namespace ChitChatApp.Api.Models.DTOs{
    public class BotChatDto
    {
        public long Id { get; set; }
        public string UserMessage { get; set; } = string.Empty;
        public string BotResponse { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public BotDto Bot { get; set; } = null!;
    }
}