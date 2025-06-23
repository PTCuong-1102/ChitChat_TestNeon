// AuthResponseDto.cs - What we send back after successful authentication
namespace ChitChatApp.Api.Models.DTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;
    }
}