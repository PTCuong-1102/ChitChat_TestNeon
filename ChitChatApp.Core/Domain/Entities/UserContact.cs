// UserContact.cs - Represents a user's contact list
namespace ChitChatApp.Core.Domain.Entities
{
    public class UserContact
    {
        public Guid Id { get; set; } // Primary key, UUID

        public Guid UserId { get; set; } // The user who added the contact

        public Guid ContactId { get; set; } // The user who was added as a contact

        public DateTime AddedAt { get; set; } // When the contact was added

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual User Contact { get; set; } = null!;
    }
}
