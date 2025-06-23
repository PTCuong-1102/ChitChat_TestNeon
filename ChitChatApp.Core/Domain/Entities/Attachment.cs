// Attachment.cs - Represents files attached to messages
using System.ComponentModel.DataAnnotations;

namespace ChitChatApp.Core.Domain.Entities
{
    public class Attachment
    {
        public Guid Id { get; set; } // Primary key, UUID

        public long MessageId { get; set; } // Which message this attachment belongs to

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty; // Original filename

        [Required]
        [StringLength(500)]
        public string FileUrl { get; set; } = string.Empty; // Where the file is stored

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty; // MIME type (image/jpeg, etc.)

        public long FileSize { get; set; } // Size in bytes

        public DateTime UploadedAt { get; set; }

        // Navigation property
        public virtual Message Message { get; set; } = null!;
    }
}
