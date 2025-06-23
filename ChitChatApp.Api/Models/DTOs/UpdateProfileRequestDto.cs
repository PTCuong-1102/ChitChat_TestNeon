using System.ComponentModel.DataAnnotations;

namespace ChitChatApp.Api.Models.DTOs
{
    public class UpdateProfileRequestDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Bio { get; set; }
        
        [Url]
        public string? AvatarUrl { get; set; }
    }
}
