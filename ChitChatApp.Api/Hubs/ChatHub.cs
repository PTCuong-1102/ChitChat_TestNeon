using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ChitChatApp.Core.Infrastructure.Persistence;
using ChitChatApp.Core.Domain.Entities;
using ChitChatApp.Api.Models.DTOs;

namespace ChitChatApp.Api.Hubs
{
    /// <summary>
    /// SignalR Hub that manages real-time chat communication
    /// Think of this as the central switchboard that routes messages between connected users
    /// The [Authorize] attribute ensures only authenticated users can connect
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatHub> _logger;

        // Constructor injection - SignalR automatically provides these dependencies
        public ChatHub(ApplicationDbContext context, ILogger<ChatHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Called automatically when a client connects to the hub
        /// This is like someone walking into a building - we want to log their arrival
        /// and update their online status
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            try
            {
                // Get the user's ID from their JWT token claims
                var userId = GetCurrentUserId();
                if (userId != null)
                {
                    // Update the user's online status in the database
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.IsOnline = true;
                        user.LastSeenAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("User {UserId} connected to ChatHub", userId);
                        
                        // Notify other users that this user came online
                        // We'll broadcast to all users who have this person as a contact
                        await NotifyContactsOfOnlineStatus(userId.Value, true);
                    }
                }

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync for connection {ConnectionId}", Context.ConnectionId);
            }
        }

        /// <summary>
        /// Called automatically when a client disconnects from the hub
        /// This handles both intentional disconnections (user closes app) and 
        /// unexpected ones (network issues, browser crash, etc.)
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId != null)
                {
                    // Update the user's offline status
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.IsOnline = false;
                        user.LastSeenAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("User {UserId} disconnected from ChatHub", userId);
                        
                        // Notify contacts that this user went offline
                        await NotifyContactsOfOnlineStatus(userId.Value, false);
                    }
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnectedAsync for connection {ConnectionId}", Context.ConnectionId);
            }
        }

        /// <summary>
        /// Adds the current user to a SignalR group representing a chat room
        /// Think of this like entering a specific room in a building - once you're in the room,
        /// you can hear all conversations happening in that room
        /// </summary>
        /// <param name="roomId">The ID of the chat room to join</param>
        public async Task JoinRoom(string roomId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    await Clients.Caller.SendAsync("Error", "User not authenticated");
                    return;
                }

                // Validate that the room exists and the user is authorized to join it
                if (!Guid.TryParse(roomId, out var roomGuid))
                {
                    await Clients.Caller.SendAsync("Error", "Invalid room ID format");
                    return;
                }

                // Check if the user is actually a participant in this room
                var isParticipant = await _context.RoomParticipants
                    .AnyAsync(rp => rp.RoomId == roomGuid && rp.UserId == userId);

                if (!isParticipant)
                {
                    await Clients.Caller.SendAsync("Error", "You are not authorized to join this room");
                    return;
                }

                // Add the connection to the SignalR group for this room
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                
                _logger.LogInformation("User {UserId} joined room {RoomId}", userId, roomId);
                
                // Notify the user that they successfully joined the room
                await Clients.Caller.SendAsync("JoinedRoom", roomId);
                
                // Optionally, notify other users in the room that someone joined
                await Clients.OthersInGroup(roomId).SendAsync("UserJoinedRoom", new
                {
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining room {RoomId} for user {UserId}", roomId, GetCurrentUserId());
                await Clients.Caller.SendAsync("Error", "Failed to join room");
            }
        }

        /// <summary>
        /// Removes the current user from a SignalR group
        /// This is like leaving a room - you stop receiving messages from that room
        /// </summary>
        /// <param name="roomId">The ID of the room to leave</param>
        public async Task LeaveRoom(string roomId)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
                
                var userId = GetCurrentUserId();
                _logger.LogInformation("User {UserId} left room {RoomId}", userId, roomId);
                
                // Notify the user that they left the room
                await Clients.Caller.SendAsync("LeftRoom", roomId);
                
                // Notify other users in the room that someone left
                await Clients.OthersInGroup(roomId).SendAsync("UserLeftRoom", new
                {
                    UserId = userId,
                    LeftAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving room {RoomId} for user {UserId}", roomId, GetCurrentUserId());
            }
        }

        /// <summary>
        /// Handles sending a message to a chat room
        /// This is the core functionality - when a user types a message and hits send,
        /// this method processes it and broadcasts it to all other users in the room
        /// </summary>
        /// <param name="roomId">The room where the message is being sent</param>
        /// <param name="content">The actual message content</param>
        /// <param name="replyToId">Optional: ID of the message this is replying to</param>
        public async Task SendMessage(string roomId, string content, long? replyToId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    await Clients.Caller.SendAsync("Error", "User not authenticated");
                    return;
                }

                // Validate input parameters
                if (string.IsNullOrWhiteSpace(content))
                {
                    await Clients.Caller.SendAsync("Error", "Message content cannot be empty");
                    return;
                }

                if (!Guid.TryParse(roomId, out var roomGuid))
                {
                    await Clients.Caller.SendAsync("Error", "Invalid room ID format");
                    return;
                }

                // Verify the user is authorized to send messages to this room
                var participant = await _context.RoomParticipants
                    .Include(rp => rp.User)
                    .Include(rp => rp.Room)
                    .FirstOrDefaultAsync(rp => rp.RoomId == roomGuid && rp.UserId == userId);

                if (participant == null)
                {
                    await Clients.Caller.SendAsync("Error", "You are not authorized to send messages to this room");
                    return;
                }

                // If this is a reply, validate that the original message exists and is in the same room
                Message? replyToMessage = null;
                if (replyToId.HasValue)
                {
                    replyToMessage = await _context.Messages
                        .FirstOrDefaultAsync(m => m.Id == replyToId.Value && m.RoomId == roomGuid);
                    
                    if (replyToMessage == null)
                    {
                        await Clients.Caller.SendAsync("Error", "Reply target message not found");
                        return;
                    }
                }

                // Create the new message entity
                var message = new Message
                {
                    RoomId = roomGuid,
                    SenderId = userId.Value,
                    Content = content.Trim(),
                    SentAt = DateTime.UtcNow,
                    ReplyToId = replyToId
                };

                // Save the message to the database
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Reload the message with all related data for broadcasting
                var savedMessage = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.ReplyTo)
                        .ThenInclude(rm => rm!.Sender)
                    .FirstOrDefaultAsync(m => m.Id == message.Id);

                if (savedMessage != null)
                {
                    // Create a DTO for the message that's safe to send to clients
                    var messageDto = new MessageDto
                    {
                        Id = savedMessage.Id,
                        RoomId = savedMessage.RoomId,
                        Content = savedMessage.Content,
                        SentAt = savedMessage.SentAt,
                        EditedAt = savedMessage.EditedAt,
                        Sender = new UserDto
                        {
                            Id = savedMessage.Sender.Id,
                            UserName = savedMessage.Sender.UserName,
                            FullName = savedMessage.Sender.FullName,
                            AvatarUrl = savedMessage.Sender.AvatarUrl,
                            IsOnline = savedMessage.Sender.IsOnline
                        },
                        ReplyTo = savedMessage.ReplyTo != null ? new MessageDto
                        {
                            Id = savedMessage.ReplyTo.Id,
                            Content = savedMessage.ReplyTo.Content,
                            SentAt = savedMessage.ReplyTo.SentAt,
                            Sender = new UserDto
                            {
                                Id = savedMessage.ReplyTo.Sender.Id,
                                UserName = savedMessage.ReplyTo.Sender.UserName,
                                FullName = savedMessage.ReplyTo.Sender.FullName,
                                AvatarUrl = savedMessage.ReplyTo.Sender.AvatarUrl
                            }
                        } : null
                    };

                    // Broadcast the message to all users in the room
                    await Clients.Group(roomId).SendAsync("ReceiveMessage", messageDto);
                    
                    // Create message status records for all participants (except the sender)
                    await CreateMessageStatusRecords(savedMessage.Id, roomGuid, userId.Value);
                    
                    _logger.LogInformation("Message {MessageId} sent by user {UserId} to room {RoomId}", 
                        savedMessage.Id, userId, roomId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to room {RoomId} from user {UserId}", roomId, GetCurrentUserId());
                await Clients.Caller.SendAsync("Error", "Failed to send message");
            }
        }

        /// <summary>
        /// Handles typing indicators - when a user starts typing, notify others in the room
        /// This creates the "User is typing..." effect that modern chat apps have
        /// </summary>
        /// <param name="roomId">The room where the user is typing</param>
        /// <param name="isTyping">Whether the user is currently typing</param>
        public async Task SetTypingStatus(string roomId, bool isTyping)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return;

                var user = await _context.Users.FindAsync(userId);
                if (user == null) return;

                // Notify other users in the room about the typing status
                await Clients.OthersInGroup(roomId).SendAsync("UserTypingStatus", new
                {
                    UserId = userId,
                    UserName = user.UserName,
                    FullName = user.FullName,
                    IsTyping = isTyping,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting typing status for user {UserId} in room {RoomId}", GetCurrentUserId(), roomId);
            }
        }

        /// <summary>
        /// Marks messages as read by the current user
        /// This is essential for implementing read receipts and unread message counts
        /// </summary>
        /// <param name="messageIds">Array of message IDs to mark as read</param>
        public async Task MarkMessagesAsRead(long[] messageIds)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return;

                // Update message status records to mark messages as read
                var statusRecords = await _context.MessageStatuses
                    .Where(ms => messageIds.Contains(ms.MessageId) && ms.UserId == userId)
                    .ToListAsync();

                foreach (var status in statusRecords)
                {
                    if (!status.IsRead)
                    {
                        status.IsRead = true;
                        status.ReadAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                // Notify the sender(s) that their messages have been read
                var messages = await _context.Messages
                    .Where(m => messageIds.Contains(m.Id))
                    .Select(m => new { m.Id, m.SenderId, m.RoomId })
                    .ToListAsync();

                foreach (var message in messages)
                {
                    // Send read receipt to the message sender
                    await Clients.User(message.SenderId.ToString()).SendAsync("MessageRead", new
                    {
                        MessageId = message.Id,
                        ReadBy = userId,
                        ReadAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking messages as read for user {UserId}", GetCurrentUserId());
            }
        }

        /// <summary>
        /// Helper method to get the current authenticated user's ID from JWT claims
        /// This extracts the user ID from the authentication token that was validated
        /// when the user connected to the hub
        /// </summary>
        /// <returns>User ID if authenticated, null otherwise</returns>
        private Guid? GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? Context.User?.FindFirst("sub")?.Value  // "sub" is standard JWT claim for user ID
                            ?? Context.User?.FindFirst("userId")?.Value; // Custom claim name if you use it
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogWarning("No user ID found in claims for connection {ConnectionId}", Context.ConnectionId);
                return null;
            }

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            _logger.LogWarning("Invalid user ID format in claims: {UserIdClaim} for connection {ConnectionId}", 
                userIdClaim, Context.ConnectionId);
            return null;
        }

        /// <summary>
        /// Notifies all contacts of a user when their online status changes
        /// This is essential for real-time presence indicators (green dot for online users)
        /// </summary>
        /// <param name="userId">The user whose status changed</param>
        /// <param name="isOnline">Whether the user is now online or offline</param>
        private async Task NotifyContactsOfOnlineStatus(Guid userId, bool isOnline)
        {
            try
            {
                // Find all users who have this person as a contact
                // We need to notify both directions - people who added this user AND people this user added
                var contactUserIds = await _context.UserContacts
                    .Where(uc => uc.ContactId == userId || uc.UserId == userId)
                    .Select(uc => uc.ContactId == userId ? uc.UserId : uc.ContactId)
                    .Distinct()
                    .ToListAsync();

                var user = await _context.Users.FindAsync(userId);
                if (user == null) return;

                // Prepare the status update message
                var statusUpdate = new
                {
                    UserId = userId,
                    UserName = user.UserName,
                    FullName = user.FullName,
                    IsOnline = isOnline,
                    LastSeenAt = user.LastSeenAt,
                    Timestamp = DateTime.UtcNow
                };

                // Send status update to each contact
                foreach (var contactUserId in contactUserIds)
                {
                    await Clients.User(contactUserId.ToString()).SendAsync("ContactStatusChanged", statusUpdate);
                }

                _logger.LogDebug("Notified {ContactCount} contacts about status change for user {UserId}", 
                    contactUserIds.Count, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying contacts of status change for user {UserId}", userId);
            }
        }

        /// <summary>
        /// Creates message status records for all participants in a room (except the sender)
        /// This tracks delivery and read status for each message per user
        /// Essential for implementing read receipts and message status indicators
        /// </summary>
        /// <param name="messageId">The ID of the message that was sent</param>
        /// <param name="roomId">The room where the message was sent</param>
        /// <param name="senderId">The ID of the user who sent the message</param>
        private async Task CreateMessageStatusRecords(long messageId, Guid roomId, Guid senderId)
        {
            try
            {
                // Get all participants in the room except the sender
                var participantIds = await _context.RoomParticipants
                    .Where(rp => rp.RoomId == roomId && rp.UserId != senderId)
                    .Select(rp => rp.UserId)
                    .ToListAsync();

                // Create a status record for each participant
                var statusRecords = participantIds.Select(participantId => new MessageStatus
                {
                    MessageId = messageId,
                    UserId = participantId,
                    IsDelivered = true, // Assume delivered since they're in the SignalR group
                    DeliveredAt = DateTime.UtcNow,
                    IsRead = false // Will be updated when they actually read the message
                }).ToList();

                if (statusRecords.Any())
                {
                    _context.MessageStatuses.AddRange(statusRecords);
                    await _context.SaveChangesAsync();

                    _logger.LogDebug("Created {StatusCount} message status records for message {MessageId}", 
                        statusRecords.Count, messageId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating message status records for message {MessageId}", messageId);
            }
        }
    }
}
