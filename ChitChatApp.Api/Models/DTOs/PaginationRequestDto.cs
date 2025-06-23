namespace ChitChatApp.Api.Models.DTOs{
    public class PaginationRequestDto
    {
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 20;
    }
}