using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ChitChatApp.Core.Contracts.Repositories;
using ChitChatApp.Api.Models.DTOs;

namespace ChitChatApp.Api.Controllers
{
    /// <summary>
    /// Controller for managing users and user operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserRepository userRepository,
            IRoomRepository roomRepository,
            ILogger<UsersController> logger)
        {
            _userRepository = userRepository;
            _roomRepository = roomRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get current user's profile
        /// GET /api/users/me
        /// </summary>
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var user = await _userRepository.GetUserByIdAsync(userId.Value);
            if (user == null) return NotFound();

            return Ok(MapUserToDto(user));
        }

        /// <summary>
        /// Update current user's profile
        /// PUT /api/users/me
        /// </summary>
        [HttpPut("me")]
        public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId.Value);
                if (user == null) return NotFound();

                // Update allowed fields
                user.FullName = request.FullName.Trim();
                user.Bio = request.Bio?.Trim();
                user.AvatarUrl = request.AvatarUrl?.Trim();

                var updatedUser = await _userRepository.UpdateUserAsync(user);
                return Ok(MapUserToDto(updatedUser));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return StatusCode(500, new { message = "Failed to update profile" });
            }
        }

        /// <summary>
        /// Search for users
        /// GET /api/users/search
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<UserDto>>> SearchUsers([FromQuery] string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
                return BadRequest("Search term must be at least 2 characters");

            var users = await _userRepository.SearchUsersAsync(searchTerm);
            var userDtos = users.Select(MapUserToDto).ToList();

            return Ok(userDtos);
        }

        /// <summary>
        /// Get a user by ID
        /// GET /api/users/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(Guid id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null) return NotFound();

            return Ok(MapUserToDto(user));
        }

        /// <summary>
        /// Start a private chat with another user
        /// POST /api/users/{id}/chat
        /// </summary>
        [HttpPost("{id}/chat")]
        public async Task<ActionResult<RoomDto>> StartPrivateChat(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (id == userId) return BadRequest("Cannot start a chat with yourself");

            try
            {
                var otherUser = await _userRepository.GetUserByIdAsync(id);
                if (otherUser == null) return NotFound("User not found");

                var room = await _roomRepository.GetOrCreatePrivateRoomAsync(userId.Value, id);
                if (room == null) return StatusCode(500, "Failed to create or retrieve private chat");

                return Ok(MapRoomToDto(room, 0));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting private chat between {UserId} and {OtherId}", userId, id);
                return StatusCode(500, new { message = "Failed to start private chat" });
            }
        }

        /// <summary>
        /// Get online users
        /// GET /api/users/online
        /// </summary>
        [HttpGet("online")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetOnlineUsers()
        {
            var users = await _userRepository.GetOnlineUsersAsync();
            var userDtos = users.Select(MapUserToDto).ToList();

            return Ok(userDtos);
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private static UserDto MapUserToDto(Core.Domain.Entities.User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FullName = user.FullName,
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl,
                IsOnline = user.IsOnline,
                CreatedAt = user.CreatedAt,
                LastSeenAt = user.LastSeenAt
            };
        }

        private static RoomDto MapRoomToDto(Core.Domain.Entities.ChatRoom room, int unreadCount)
        {
            return new RoomDto
            {
                Id = room.Id,
                Name = room.Name,
                Description = room.Description,
                IsPrivate = room.IsPrivate,
                CreatedAt = room.CreatedAt,
                UpdatedAt = room.UpdatedAt,
                UnreadCount = unreadCount,
                Participants = room.Participants.Select(p => new RoomParticipantDto
                {
                    UserId = p.UserId,
                    UserName = p.User.UserName,
                    FullName = p.User.FullName,
                    AvatarUrl = p.User.AvatarUrl,
                    IsAdmin = p.IsAdmin,
                    IsOnline = p.User.IsOnline,
                    JoinedAt = p.JoinedAt
                }).ToList()
            };
        }
    }
}