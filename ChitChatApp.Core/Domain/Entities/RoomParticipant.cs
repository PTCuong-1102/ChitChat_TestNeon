// RoomParticipant.cs - Junction table linking users to chat rooms
namespace ChitChatApp.Core.Domain.Entities
{
    public class RoomParticipant
    {
        public int Id { get; set; } // Primary key, auto-incrementing

        public Guid RoomId { get; set; } // Foreign key to ChatRoom

        public Guid UserId { get; set; } // Foreign key to User

        public DateTime JoinedAt { get; set; } // When the user joined this room

        public bool IsAdmin { get; set; } = false; // Whether user has admin privileges in this room

        // Navigation properties
        public virtual ChatRoom Room { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
