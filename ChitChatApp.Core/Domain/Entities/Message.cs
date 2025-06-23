// Message.cs - Represents a chat message
using System.ComponentModel.DataAnnotations;

namespace ChitChatApp.Core.Domain.Entities
{
    public class Message
    {
        public long Id { get; set; } // Primary key, auto-incrementing long for high volume

        public Guid RoomId { get; set; } // Which room this message belongs to

        public Guid SenderId { get; set; } // Who sent the message

        [Required]
        public string Content { get; set; } = string.Empty; // The actual message text

        public DateTime SentAt { get; set; } // When the message was sent

        public DateTime? EditedAt { get; set; } // When the message was last edited (if ever)

        public long? ReplyToId { get; set; } // If this message is a reply, reference to original message

        // Navigation properties
        public virtual ChatRoom Room { get; set; } = null!;
        public virtual User Sender { get; set; } = null!;
        public virtual Message? ReplyTo { get; set; } // The message this is replying to
        public virtual ICollection<Message> Replies { get; set; } = new List<Message>(); // Messages replying to this one
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        public virtual ICollection<MessageStatus> MessageStatuses { get; set; } = new List<MessageStatus>();
    }
}
