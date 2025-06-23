// ChatRoom.cs - Represents a chat room where users can communicate
using System.ComponentModel.DataAnnotations;

namespace ChitChatApp.Core.Domain.Entities
{
    public class ChatRoom
    {
        public Guid Id { get; set; } // Primary key, auto-generated UUID

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; } // Optional room description

        public bool IsPrivate { get; set; } = false; // True for direct messages, false for group chats

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<RoomParticipant> Participants { get; set; } = new List<RoomParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}