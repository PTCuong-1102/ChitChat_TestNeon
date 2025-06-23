using System.ComponentModel.DataAnnotations;

namespace ChitChatApp.Api.Models.DTOs
{
    // Room-related DTOs
    public class CreateRoomRequestDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public bool IsPrivate { get; set; } = false;
        
        public List<Guid> InitialParticipants { get; set; } = new List<Guid>();
    }
}