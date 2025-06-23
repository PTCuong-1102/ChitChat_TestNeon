using System.ComponentModel.DataAnnotations;

namespace ChitChatApp.Api.Models.DTOs{
    public class SearchUsersRequestDto
    {
        [Required]
        [MinLength(2)]
        public string SearchTerm { get; set; } = string.Empty;
    }
}