// LoginRequestDto.cs - What the client sends when logging in
using System.ComponentModel.DataAnnotations;

namespace ChitChatApp.Api.Models.DTOs
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }
}