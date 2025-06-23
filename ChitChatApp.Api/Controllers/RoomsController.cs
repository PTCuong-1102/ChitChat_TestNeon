using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ChitChatApp.Core.Contracts.Repositories;
using ChitChatApp.Api.Models.DTOs;

namespace ChitChatApp.Api.Controllers
{
    /// <summary>
    /// Controller for managing chat rooms and room membership
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly ILogger<RoomsController> _logger;

        public RoomsController(
            IRoomRepository roomRepository,
            IMessageRepository messageRepository,
            ILogger<RoomsController> logger)
        {
            _roomRepository = roomRepository;
            _messageRepository = messageRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get all rooms for the current user
        /// GET /api/rooms
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomDto>>> GetUserRooms()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var rooms = await _roomRepository.GetRoomsForUserAsync(userId.Value);
            var roomDtos = new List<RoomDto>();

            foreach (var room in rooms)
            {
                var unreadCount = await _messageRepository.GetUnreadMessageCountAsync(userId.Value, room.Id);
                var roomDto = MapRoomToDto(room, unreadCount);
                roomDtos.Add(roomDto);
            }

            return Ok(roomDtos);
        }

        /// <summary>
        /// Get a specific room by ID
        /// GET /api/rooms/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomDto>> GetRoom(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            // Check if user is a participant in this room
            var isParticipant = await _roomRepository.IsUserInRoomAsync(id, userId.Value);
            if (!isParticipant) return Forbid();

            var room = await _roomRepository.GetRoomWithParticipantsAsync(id);
            if (room == null) return NotFound();

            var unreadCount = await _messageRepository.GetUnreadMessageCountAsync(userId.Value, room.Id);
            return Ok(MapRoomToDto(room, unreadCount));
        }

        /// <summary>
        /// Create a new chat room
        /// POST /api/rooms
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<RoomDto>> CreateRoom([FromBody] CreateRoomRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var room = new Core.Domain.Entities.ChatRoom
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    IsPrivate = request.IsPrivate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdRoom = await _roomRepository.CreateRoomAsync(room);

                // Add the creator as an admin
                await _roomRepository.AddUserToRoomAsync(createdRoom.Id, userId.Value, isAdmin: true);

                // Add initial participants if specified
                foreach (var participantId in request.InitialParticipants)
                {
                    if (participantId != userId.Value) // Don't add creator twice
                    {
                        await _roomRepository.AddUserToRoomAsync(createdRoom.Id, participantId);
                    }
                }

                // Reload room with participants
                var roomWithParticipants = await _roomRepository.GetRoomWithParticipantsAsync(createdRoom.Id);
                return CreatedAtAction(nameof(GetRoom), new { id = createdRoom.Id }, MapRoomToDto(roomWithParticipants!, 0));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating room for user {UserId}", userId);
                return StatusCode(500, new { message = "Failed to create room" });
            }
        }

        /// <summary>
        /// Add a user to a room
        /// POST /api/rooms/{id}/participants
        /// </summary>
        [HttpPost("{id}/participants")]
        public async Task<ActionResult> AddParticipant(Guid id, [FromBody] AddContactRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            // Check if current user is an admin of this room
            var room = await _roomRepository.GetRoomWithParticipantsAsync(id);
            if (room == null) return NotFound();

            var currentUserParticipant = room.Participants.FirstOrDefault(p => p.UserId == userId);
            if (currentUserParticipant == null || !currentUserParticipant.IsAdmin)
                return Forbid("Only room administrators can add participants");

            var success = await _roomRepository.AddUserToRoomAsync(id, request.ContactId);
            if (!success) return BadRequest("User is already a participant or operation failed");

            return Ok(new { message = "Participant added successfully" });
        }

        /// <summary>
        /// Remove a user from a room
        /// DELETE /api/rooms/{id}/participants/{userId}
        /// </summary>
        [HttpDelete("{id}/participants/{participantId}")]
        public async Task<ActionResult> RemoveParticipant(Guid id, Guid participantId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var room = await _roomRepository.GetRoomWithParticipantsAsync(id);
            if (room == null) return NotFound();

            var currentUserParticipant = room.Participants.FirstOrDefault(p => p.UserId == userId);
            if (currentUserParticipant == null) return Forbid();

            // Users can remove themselves, or admins can remove others
            if (participantId != userId && !currentUserParticipant.IsAdmin)
                return Forbid("Only room administrators can remove other participants");

            var success = await _roomRepository.RemoveUserFromRoomAsync(id, participantId);
            if (!success) return BadRequest("User is not a participant or operation failed");

            return Ok(new { message = "Participant removed successfully" });
        }

        /// <summary>
        /// Get messages for a specific room
        /// GET /api/rooms/{id}/messages
        /// </summary>
        [HttpGet("{id}/messages")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetRoomMessages(
            Guid id, 
            [FromQuery] int skip = 0, 
            [FromQuery] int take = 50)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var isParticipant = await _roomRepository.IsUserInRoomAsync(id, userId.Value);
            if (!isParticipant) return Forbid();

            var messages = await _messageRepository.GetRoomMessagesAsync(id, skip, take);
            var messageDtos = messages.Select(MapMessageToDto).ToList();

            return Ok(messageDtos);
        }

        /// <summary>
        /// Search for public rooms
        /// GET /api/rooms/search
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<RoomDto>>> SearchRooms([FromQuery] string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
                return BadRequest("Search term must be at least 2 characters");

            var rooms = await _roomRepository.SearchRoomsAsync(searchTerm);
            var roomDtos = rooms.Select(r => MapRoomToDto(r, 0)).ToList();

            return Ok(roomDtos);
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
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
                }).ToList(),
                LastMessage = room.Messages.Any() ? MapMessageToDto(room.Messages.OrderByDescending(m => m.SentAt).First()) : null
            };
        }

        private static MessageDto MapMessageToDto(Core.Domain.Entities.Message message)
        {
            return new MessageDto
            {
                Id = message.Id,
                RoomId = message.RoomId,
                Content = message.Content,
                SentAt = message.SentAt,
                EditedAt = message.EditedAt,
                Sender = new UserDto
                {
                    Id = message.Sender.Id,
                    UserName = message.Sender.UserName,
                    FullName = message.Sender.FullName,
                    Email = message.Sender.Email,
                    AvatarUrl = message.Sender.AvatarUrl,
                    IsOnline = message.Sender.IsOnline
                },
                ReplyTo = message.ReplyTo != null ? new MessageDto
                {
                    Id = message.ReplyTo.Id,
                    Content = message.ReplyTo.Content,
                    SentAt = message.ReplyTo.SentAt,
                    Sender = new UserDto
                    {
                        Id = message.ReplyTo.Sender.Id,
                        UserName = message.ReplyTo.Sender.UserName,
                        FullName = message.ReplyTo.Sender.FullName,
                        AvatarUrl = message.ReplyTo.Sender.AvatarUrl
                    }
                } : null
            };
        }
    }
}