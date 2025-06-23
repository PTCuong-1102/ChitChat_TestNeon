namespace ChitChatApp.Api.Models.DTOs
{
    /// <summary>
    /// Data Transfer Object for messages sent through SignalR
    /// This represents a safe, cleaned version of a message that can be sent to clients
    /// </summary>
    public class MessageDto
    {
        public long Id { get; set; }
        public Guid RoomId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public UserDto Sender { get; set; } = null!;
        public MessageDto? ReplyTo { get; set; } // For threaded conversations
        public List<AttachmentDto> Attachments { get; set; } = new List<AttachmentDto>();
    }

    /// <summary>
    /// Data Transfer Object for file attachments
    /// </summary>
    public class AttachmentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}