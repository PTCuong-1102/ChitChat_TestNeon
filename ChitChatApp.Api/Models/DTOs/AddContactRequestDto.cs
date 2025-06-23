using System.ComponentModel.DataAnnotations;

namespace ChitChatApp.Api.Models.DTOs{
    // Contact-related DTOs
    public class AddContactRequestDto
    {
        [Required]
        public Guid ContactId { get; set; }
    }

}