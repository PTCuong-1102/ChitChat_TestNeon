namespace ChitChatApp.Api.Models.DTOs{
    public class RoomParticipantDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsOnline { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}