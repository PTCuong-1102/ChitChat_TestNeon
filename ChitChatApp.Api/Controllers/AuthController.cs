// AuthController.cs - Handles user registration and login

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChitChatApp.Core.Infrastructure.Persistence;
using ChitChatApp.Core.Domain.Entities;
using ChitChatApp.Api.Models.DTOs;
using ChitChatApp.Api.Configuration;

namespace ChitChatApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthController> _logger;

        // Constructor injection - ASP.NET Core automatically provides these dependencies
        public AuthController(
            ApplicationDbContext context,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user account
        /// POST /api/auth/register
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                // Step 1: Validate the incoming request
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Step 2: Check if email already exists
                // This is crucial for preventing duplicate accounts
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (existingUserByEmail != null)
                {
                    return BadRequest(new { message = "An account with this email already exists" });
                }

                // Step 3: Check if username already exists
                var existingUserByUsername = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName.ToLower() == request.UserName.ToLower());

                if (existingUserByUsername != null)
                {
                    return BadRequest(new { message = "This username is already taken" });
                }

                // Step 4: Hash the password using BCrypt
                // BCrypt is specifically designed for password hashing - it's slow by design
                // which makes it computationally expensive for attackers to crack passwords
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Step 5: Create the new user entity
                var newUser = new User
                {
                    Email = request.Email.ToLower().Trim(), // Normalize email
                    UserName = request.UserName.Trim(),
                    FullName = request.FullName.Trim(),
                    Password = hashedPassword,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsOnline = false
                };

                // Step 6: Save to database
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New user registered with email: {Email}", request.Email);

                // Step 7: Return success response (don't include sensitive information)
                var userDto = MapUserToDto(newUser);
                return CreatedAtAction(nameof(Register), new { id = newUser.Id }, new
                {
                    message = "User registered successfully",
                    user = userDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user registration for email: {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token
        /// POST /api/auth/login
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                // Step 1: Validate the request
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Step 2: Find the user by email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (user == null)
                {
                    // Don't reveal whether the email exists or not - this prevents account enumeration attacks
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Step 3: Verify the password using BCrypt
                var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);

                if (!isPasswordValid)
                {
                    _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Step 4: Update user's last seen timestamp and online status
                user.LastSeenAt = DateTime.UtcNow;
                user.IsOnline = true;
                await _context.SaveChangesAsync();

                // Step 5: Generate JWT token
                var token = GenerateJwtToken(user);
                var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

                _logger.LogInformation("User logged in successfully: {Email}", request.Email);

                // Step 6: Return the authentication response
                var response = new AuthResponseDto
                {
                    Token = token,
                    ExpiresAt = expiresAt,
                    User = MapUserToDto(user)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login for email: {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Logs out the current user (optional endpoint for updating online status)
        /// POST /api/auth/logout
        /// </summary>
        [HttpPost("logout")]
        [Microsoft.AspNetCore.Authorization.Authorize] // Requires valid JWT token
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Get the current user's ID from the JWT token claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.IsOnline = false;
                        user.LastSeenAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during logout");
                return StatusCode(500, new { message = "An error occurred during logout" });
            }
        }

        // =====================================
        // PRIVATE HELPER METHODS
        // =====================================

        /// <summary>
        /// Generates a JWT token for the authenticated user
        /// This token will be sent with every subsequent request to prove the user's identity
        /// </summary>
        private string GenerateJwtToken(User user)
        {
            // Create the claims - these are pieces of information about the user
            // that will be embedded in the token
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("FullName", user.FullName),
                // Add a unique identifier for this specific token (useful for token revocation)
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                // Add issued at timestamp
                new Claim(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            // Create the signing key from our secret
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create the token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = credentials
            };

            // Generate and return the token
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Maps a User entity to a UserDto for safe transmission to clients
        /// This removes sensitive information like passwords
        /// </summary>
        private static UserDto MapUserToDto(User user)
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
    }
}