using System.ComponentModel.DataAnnotations;

namespace ChitChatApp.Core.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; } // Primary key, auto-generated UUID

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty; // BCrypt hashed password

        public string? Bio { get; set; } // Optional user biography

        public string? AvatarUrl { get; set; } // Optional profile picture URL

        public DateTime CreatedAt { get; set; } // When the user account was created

        public DateTime UpdatedAt { get; set; } // Last profile update

        public DateTime? LastSeenAt { get; set; } // When user was last active

        public bool IsOnline { get; set; } = false; // Current online status

        // Navigation properties - these represent relationships with other entities
        public virtual ICollection<RoomParticipant> RoomParticipants { get; set; } = new List<RoomParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
        public virtual ICollection<UserContact> ContactsAdded { get; set; } = new List<UserContact>();
        public virtual ICollection<UserContact> ContactsWhoAdded { get; set; } = new List<UserContact>();
        public virtual ICollection<BlockedUser> BlockedUsers { get; set; } = new List<BlockedUser>();
        public virtual ICollection<BlockedUser> BlockedByUsers { get; set; } = new List<BlockedUser>();
        public virtual ICollection<BotChat> BotChats { get; set; } = new List<BotChat>();
    }
}