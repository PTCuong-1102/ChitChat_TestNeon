using Microsoft.EntityFrameworkCore;
using ChitChatApp.Core.Infrastructure.Persistence;
using ChitChatApp.Core.Domain.Entities;

namespace ChitChatApp.Api.Infrastructure
{
    /// <summary>
    /// Handles database initialization, migration, and seeding
    /// This class ensures your database is created and populated with initial data
    /// </summary>
    public static class DatabaseInitializer
    {
        /// <summary>
        /// Initializes the database and seeds initial data if needed
        /// This method is called during application startup
        /// </summary>
        public static async Task InitializeDatabaseAsync(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                
                logger.LogInformation("Starting database initialization...");

                // Ensure the database is created
                // This will create the database and all tables if they don't exist
                var created = await context.Database.EnsureCreatedAsync();
                
                if (created)
                {
                    logger.LogInformation("Database was created successfully. Seeding initial data...");
                    await SeedInitialDataAsync(context, logger);
                    logger.LogInformation("Database seeding completed successfully.");
                }
                else
                {
                    logger.LogInformation("Database already exists. Checking if seeding is needed...");
                    
                    // Check if we need to seed data (useful for development)
                    var hasUsers = await context.Users.AnyAsync();
                    if (!hasUsers)
                    {
                        logger.LogInformation("No users found. Seeding initial data...");
                        await SeedInitialDataAsync(context, logger);
                        logger.LogInformation("Database seeding completed successfully.");
                    }
                    else
                    {
                        logger.LogInformation("Database already contains data. Skipping seeding.");
                    }
                }

                logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
                throw; // Re-throw to prevent application startup with broken database
            }
        }

        /// <summary>
        /// Seeds the database with initial demo data
        /// This creates a demo user, a general chat room, and demo bots
        /// </summary>
        private static async Task SeedInitialDataAsync(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                // Create demo user with hashed password
                logger.LogInformation("Creating demo user...");
                
                var demoUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = "demo@chitchatapp.com",
                    UserName = "demo_user",
                    FullName = "Demo User",
                    Password = BCrypt.Net.BCrypt.HashPassword("demo123"), // Hash the password
                    Bio = "This is a demo user account for testing ChitChatApp. Feel free to send me messages!",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsOnline = false
                };

                context.Users.Add(demoUser);
                await context.SaveChangesAsync();
                logger.LogInformation("Demo user created with ID: {UserId}", demoUser.Id);

                // Create admin user (for system administration)
                logger.LogInformation("Creating admin user...");
                
                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = "admin@chitchatapp.com",
                    UserName = "admin",
                    FullName = "System Administrator",
                    Password = BCrypt.Net.BCrypt.HashPassword("admin123"), // Change this in production!
                    Bio = "System administrator account. Contact me for any issues or support needs.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsOnline = false
                };

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
                logger.LogInformation("Admin user created with ID: {UserId}", adminUser.Id);

                // Create the "General Chat" public room
                logger.LogInformation("Creating General Chat room...");
                
                var generalChatRoom = new ChatRoom
                {
                    Id = Guid.NewGuid(),
                    Name = "General Chat",
                    Description = "Welcome to ChitChatApp! This is the main public chat room where everyone can connect and chat. Feel free to introduce yourself and start conversations!",
                    IsPrivate = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.ChatRooms.Add(generalChatRoom);
                await context.SaveChangesAsync();
                logger.LogInformation("General Chat room created with ID: {RoomId}", generalChatRoom.Id);

                // Add both users to the General Chat room
                logger.LogInformation("Adding users to General Chat room...");
                
                var demoParticipant = new RoomParticipant
                {
                    RoomId = generalChatRoom.Id,
                    UserId = demoUser.Id,
                    JoinedAt = DateTime.UtcNow,
                    IsAdmin = false
                };

                var adminParticipant = new RoomParticipant
                {
                    RoomId = generalChatRoom.Id,
                    UserId = adminUser.Id,
                    JoinedAt = DateTime.UtcNow,
                    IsAdmin = true // Admin is a room administrator
                };

                context.RoomParticipants.AddRange(demoParticipant, adminParticipant);
                await context.SaveChangesAsync();
                logger.LogInformation("Users added to General Chat room successfully.");

                // Create welcome message in General Chat
                logger.LogInformation("Creating welcome message...");
                
                var welcomeMessage = new Message
                {
                    RoomId = generalChatRoom.Id,
                    SenderId = adminUser.Id,
                    Content = "Welcome to ChitChatApp! 🎉 This is your first chat room. You can create private rooms, add contacts, and even chat with bots. Try typing a message to get started!",
                    SentAt = DateTime.UtcNow
                };

                context.Messages.Add(welcomeMessage);
                await context.SaveChangesAsync();
                logger.LogInformation("Welcome message created with ID: {MessageId}", welcomeMessage.Id);

                // Create the Echo Bot
                logger.LogInformation("Creating Echo Bot...");
                
                var echoBot = new Bot
                {
                    Id = Guid.NewGuid(),
                    Name = "Echo Bot",
                    Description = "A simple bot that echoes back everything you say. Perfect for testing the bot functionality!",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Bots.Add(echoBot);
                await context.SaveChangesAsync();
                logger.LogInformation("Echo Bot created with ID: {BotId}", echoBot.Id);

                // Create Welcome Bot
                logger.LogInformation("Creating Welcome Bot...");
                
                var welcomeBot = new Bot
                {
                    Id = Guid.NewGuid(),
                    Name = "Welcome Bot",
                    Description = "A friendly bot that welcomes new users and helps them get started with ChitChatApp.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Bots.Add(welcomeBot);
                await context.SaveChangesAsync();
                logger.LogInformation("Welcome Bot created with ID: {BotId}", welcomeBot.Id);

                // Create Help Bot
                logger.LogInformation("Creating Help Bot...");
                
                var helpBot = new Bot
                {
                    Id = Guid.NewGuid(),
                    Name = "Help Bot",
                    Description = "Get help and learn about ChitChatApp features. Ask me anything about how to use the application!",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Bots.Add(helpBot);
                await context.SaveChangesAsync();
                logger.LogInformation("Help Bot created with ID: {BotId}", helpBot.Id);

                // Create sample bot interactions for demo user
                logger.LogInformation("Creating sample bot interactions...");
                
                var sampleBotChats = new List<BotChat>
                {
                    new BotChat
                    {
                        BotId = welcomeBot.Id,
                        UserId = demoUser.Id,
                        UserMessage = "Hello!",
                        BotResponse = "Welcome to ChitChatApp! I'm here to help you get started. You can create chat rooms, add friends, and explore all the features. What would you like to do first?",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-30)
                    },
                    new BotChat
                    {
                        BotId = echoBot.Id,
                        UserId = demoUser.Id,
                        UserMessage = "Testing echo functionality",
                        BotResponse = "You said: Testing echo functionality",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-20)
                    },
                    new BotChat
                    {
                        BotId = helpBot.Id,
                        UserId = demoUser.Id,
                        UserMessage = "How do I create a chat room?",
                        BotResponse = "To create a chat room, you can use the API endpoint POST /api/rooms with a room name and description. You can also make it private if you want. Would you like me to explain more features?",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-10)
                    }
                };

                context.BotChats.AddRange(sampleBotChats);
                await context.SaveChangesAsync();
                logger.LogInformation("Sample bot interactions created successfully.");

                // Create a second demo room for testing private rooms
                logger.LogInformation("Creating Demo Private Room...");
                
                var privateRoom = new ChatRoom
                {
                    Id = Guid.NewGuid(),
                    Name = "Private Chat",
                    Description = "This is a private chat room between demo users.",
                    IsPrivate = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.ChatRooms.Add(privateRoom);
                await context.SaveChangesAsync();

                // Add both demo users to the private room
                var privateParticipants = new List<RoomParticipant>
                {
                    new RoomParticipant
                    {
                        RoomId = privateRoom.Id,
                        UserId = demoUser.Id,
                        JoinedAt = DateTime.UtcNow,
                        IsAdmin = true
                    },
                    new RoomParticipant
                    {
                        RoomId = privateRoom.Id,
                        UserId = adminUser.Id,
                        JoinedAt = DateTime.UtcNow,
                        IsAdmin = false
                    }
                };

                context.RoomParticipants.AddRange(privateParticipants);
                await context.SaveChangesAsync();
                logger.LogInformation("Demo Private Room created and users added.");

                // Add them as contacts to each other
                logger.LogInformation("Creating contact relationships...");
                
                var contactRelationships = new List<UserContact>
                {
                    new UserContact
                    {
                        UserId = demoUser.Id,
                        ContactId = adminUser.Id,
                        AddedAt = DateTime.UtcNow
                    },
                    new UserContact
                    {
                        UserId = adminUser.Id,
                        ContactId = demoUser.Id,
                        AddedAt = DateTime.UtcNow
                    }
                };

                context.UserContacts.AddRange(contactRelationships);
                await context.SaveChangesAsync();
                logger.LogInformation("Contact relationships created successfully.");

                logger.LogInformation("Database seeding completed successfully!");
                
                // Log summary of created data
                var userCount = await context.Users.CountAsync();
                var roomCount = await context.ChatRooms.CountAsync();
                var botCount = await context.Bots.CountAsync();
                var messageCount = await context.Messages.CountAsync();
                
                logger.LogInformation("Seeding Summary: {UserCount} users, {RoomCount} rooms, {BotCount} bots, {MessageCount} messages created.", 
                    userCount, roomCount, botCount, messageCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during database seeding.");
                throw;
            }
        }
    }
}