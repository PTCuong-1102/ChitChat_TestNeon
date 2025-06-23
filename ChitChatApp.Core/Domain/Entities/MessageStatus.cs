// MessageStatus.cs - Tracks read/delivery status of messages for each user
namespace ChitChatApp.Core.Domain.Entities
{
    public class MessageStatus
    {
        public Guid Id { get; set; } // Primary key, UUID

        public long MessageId { get; set; } // Which message this status refers to

        public Guid UserId { get; set; } // Which user this status is for

        public bool IsDelivered { get; set; } = false; // Has the message been delivered to this user?

        public bool IsRead { get; set; } = false; // Has this user read the message?

        public DateTime? DeliveredAt { get; set; } // When it was delivered

        public DateTime? ReadAt { get; set; } // When it was read

        // Navigation properties
        public virtual Message Message { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
