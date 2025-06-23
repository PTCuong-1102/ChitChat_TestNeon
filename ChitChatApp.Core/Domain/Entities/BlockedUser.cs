// BlockedUser.cs - Represents blocked users
namespace ChitChatApp.Core.Domain.Entities
{
    public class BlockedUser
    {
        public Guid Id { get; set; } // Primary key, UUID

        public Guid UserId { get; set; } // The user who blocked someone

        public Guid BlockedUserId { get; set; } // The user who was blocked

        public DateTime BlockedAt { get; set; } // When the block occurred

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual User BlockedUserEntity { get; set; } = null!;
    }
}