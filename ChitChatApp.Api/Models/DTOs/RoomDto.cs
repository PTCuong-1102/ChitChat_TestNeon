namespace ChitChatApp.Api.Models.DTOs{
    public class RoomDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPrivate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<RoomParticipantDto> Participants { get; set; } = new List<RoomParticipantDto>();
        public MessageDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }
}