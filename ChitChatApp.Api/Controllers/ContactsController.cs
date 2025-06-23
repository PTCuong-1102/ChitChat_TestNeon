using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ChitChatApp.Core.Contracts.Repositories;
using ChitChatApp.Api.Models.DTOs;

namespace ChitChatApp.Api.Controllers
{
    /// <summary>
    /// Controller for managing user contacts
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContactsController : ControllerBase
    {
        private readonly IContactRepository _contactRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBlockedUserRepository _blockedUserRepository;
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(
            IContactRepository contactRepository,
            IUserRepository userRepository,
            IBlockedUserRepository blockedUserRepository,
            ILogger<ContactsController> logger)
        {
            _contactRepository = contactRepository;
            _userRepository = userRepository;
            _blockedUserRepository = blockedUserRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get current user's contacts
        /// GET /api/contacts
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContactDto>>> GetContacts()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var contacts = await _contactRepository.GetUserContactsAsync(userId.Value);
            var contactDtos = contacts.Select(c => new ContactDto
            {
                Id = c.Contact.Id,
                UserName = c.Contact.UserName,
                FullName = c.Contact.FullName,
                Email = c.Contact.Email,
                Bio = c.Contact.Bio,
                AvatarUrl = c.Contact.AvatarUrl,
                IsOnline = c.Contact.IsOnline,
                LastSeenAt = c.Contact.LastSeenAt,
                AddedAt = c.AddedAt
            }).ToList();

            return Ok(contactDtos);
        }

        /// <summary>
        /// Add a new contact
        /// POST /api/contacts
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ContactDto>> AddContact([FromBody] AddContactRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (request.ContactId == userId) return BadRequest("Cannot add yourself as a contact");

            try
            {
                // Check if contact exists
                var contactUser = await _userRepository.GetUserByIdAsync(request.ContactId);
                if (contactUser == null) return NotFound("User not found");

                // Check if already a contact
                var existingContact = await _contactRepository.GetContactAsync(userId.Value, request.ContactId);
                if (existingContact != null) return BadRequest("User is already in your contacts");

                // Check if user is blocked
                var isBlocked = await _blockedUserRepository.IsUserBlockedAsync(userId.Value, request.ContactId);
                if (isBlocked) return BadRequest("Cannot add blocked user as contact");

                var contact = await _contactRepository.AddContactAsync(userId.Value, request.ContactId);

                return CreatedAtAction(nameof(GetContacts), new ContactDto
                {
                    Id = contactUser.Id,
                    UserName = contactUser.UserName,
                    FullName = contactUser.FullName,
                    Email = contactUser.Email,
                    Bio = contactUser.Bio,
                    AvatarUrl = contactUser.AvatarUrl,
                    IsOnline = contactUser.IsOnline,
                    LastSeenAt = contactUser.LastSeenAt,
                    AddedAt = contact.AddedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding contact {ContactId} for user {UserId}", request.ContactId, userId);
                return StatusCode(500, new { message = "Failed to add contact" });
            }
        }

        /// <summary>
        /// Remove a contact
        /// DELETE /api/contacts/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> RemoveContact(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var success = await _contactRepository.RemoveContactAsync(userId.Value, id);
            if (!success) return NotFound("Contact not found");

            return Ok(new { message = "Contact removed successfully" });
        }

        /// <summary>
        /// Get mutual contacts with another user
        /// GET /api/contacts/{id}/mutual
        /// </summary>
        [HttpGet("{id}/mutual")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetMutualContacts(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var mutualContacts = await _contactRepository.GetMutualContactsAsync(userId.Value, id);
            var userDtos = mutualContacts.Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl,
                IsOnline = u.IsOnline
            }).ToList();

            return Ok(userDtos);
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}