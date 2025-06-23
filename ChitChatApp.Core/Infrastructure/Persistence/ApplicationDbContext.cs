// ApplicationDbContext.cs - The main database context that coordinates all data access
using Microsoft.EntityFrameworkCore;
using ChitChatApp.Core.Domain.Entities;

namespace ChitChatApp.Core.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        // Constructor that accepts DbContext options (connection string, etc.)
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSet properties - these represent tables in your database
        public DbSet<User> Users { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<RoomParticipant> RoomParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<MessageStatus> MessageStatuses { get; set; }
        public DbSet<UserContact> UserContacts { get; set; }
        public DbSet<BlockedUser> BlockedUsers { get; set; }
        public DbSet<Bot> Bots { get; set; }
        public DbSet<BotChat> BotChats { get; set; }

        // Helper method to check if we're using SQLite
        private bool IsUsingSqlite()
        {
            return Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";
        }

        // OnModelCreating - This is where we configure how our C# classes map to database tables
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Get appropriate SQL functions for the database provider
            var uuidGenerator = IsUsingSqlite() ? "lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || '4' || substr(hex(randomblob(2)),2) || '-' || substr('AB89',abs(random()) % 4 + 1, 1) || substr(hex(randomblob(2)),2) || '-' || hex(randomblob(6)))" : "gen_random_uuid()";
            var timestampGenerator = IsUsingSqlite() ? "datetime('now')" : "CURRENT_TIMESTAMP";

            // User configuration - This represents your users table
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users"); // Maps to the 'users' table in PostgreSQL
                entity.HasKey(e => e.Id); // Primary key
                entity.Property(e => e.Id).HasDefaultValueSql(uuidGenerator); // Auto-generate UUID

                // Unique constraints - ensures no duplicate emails or usernames
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.UserName).IsUnique();

                // Default values for timestamps
                entity.Property(e => e.CreatedAt).HasDefaultValueSql(timestampGenerator);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql(timestampGenerator);
                entity.Property(e => e.IsOnline).HasDefaultValue(false);
            });

            // ChatRoom configuration
            modelBuilder.Entity<ChatRoom>(entity =>
            {
                entity.ToTable("chat_rooms");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql(uuidGenerator);
                entity.Property(e => e.IsPrivate).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql(timestampGenerator);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql(timestampGenerator);
            });

            // RoomParticipant configuration - Junction table with composite unique constraint
            modelBuilder.Entity<RoomParticipant>(entity =>
            {
                entity.ToTable("room_participants");
                entity.HasKey(e => e.Id);

                // Composite unique constraint - a user can only be in a room once
                entity.HasIndex(p => new { p.RoomId, p.UserId }).IsUnique();

                entity.Property(e => e.JoinedAt).HasDefaultValueSql(timestampGenerator);
                entity.Property(e => e.IsAdmin).HasDefaultValue(false);

                // Foreign key relationships with cascade delete
                entity.HasOne(rp => rp.Room)
                    .WithMany(r => r.Participants)
                    .HasForeignKey(rp => rp.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.User)
                    .WithMany(u => u.RoomParticipants)
                    .HasForeignKey(rp => rp.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Message configuration
            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("messages");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SentAt).HasDefaultValueSql(timestampGenerator);

                // Foreign key relationships
                entity.HasOne(m => m.Room)
                    .WithMany(r => r.Messages)
                    .HasForeignKey(m => m.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Sender)
                    .WithMany(u => u.Messages)
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Self-referencing relationship for replies
                entity.HasOne(m => m.ReplyTo)
                    .WithMany(m => m.Replies)
                    .HasForeignKey(m => m.ReplyToId)
                    .OnDelete(DeleteBehavior.SetNull); // Don't delete replies if original message is deleted
            });

            // Attachment configuration
            modelBuilder.Entity<Attachment>(entity =>
            {
                entity.ToTable("attachments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql(uuidGenerator);
                entity.Property(e => e.UploadedAt).HasDefaultValueSql(timestampGenerator);

                entity.HasOne(a => a.Message)
                    .WithMany(m => m.Attachments)
                    .HasForeignKey(a => a.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // MessageStatus configuration
            modelBuilder.Entity<MessageStatus>(entity =>
            {
                entity.ToTable("message_status");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql(uuidGenerator);
                entity.Property(e => e.IsDelivered).HasDefaultValue(false);
                entity.Property(e => e.IsRead).HasDefaultValue(false);

                // Composite unique constraint - one status record per message per user
                entity.HasIndex(ms => new { ms.MessageId, ms.UserId }).IsUnique();

                entity.HasOne(ms => ms.Message)
                    .WithMany(m => m.MessageStatuses)
                    .HasForeignKey(ms => ms.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ms => ms.User)
                    .WithMany()
                    .HasForeignKey(ms => ms.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // UserContact configuration
            modelBuilder.Entity<UserContact>(entity =>
            {
                entity.ToTable("user_contacts");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql(uuidGenerator);
                entity.Property(e => e.AddedAt).HasDefaultValueSql(timestampGenerator);

                // Composite unique constraint - can't add same contact twice
                entity.HasIndex(uc => new { uc.UserId, uc.ContactId }).IsUnique();

                // Self-referencing relationships to User table
                entity.HasOne(uc => uc.User)
                    .WithMany(u => u.ContactsAdded)
                    .HasForeignKey(uc => uc.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(uc => uc.Contact)
                    .WithMany(u => u.ContactsWhoAdded)
                    .HasForeignKey(uc => uc.ContactId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // BlockedUser configuration
            modelBuilder.Entity<BlockedUser>(entity =>
            {
                entity.ToTable("blocked_users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql(uuidGenerator);
                entity.Property(e => e.BlockedAt).HasDefaultValueSql(timestampGenerator);

                // Composite unique constraint - can't block same user twice
                entity.HasIndex(bu => new { bu.UserId, bu.BlockedUserId }).IsUnique();

                entity.HasOne(bu => bu.User)
                    .WithMany(u => u.BlockedUsers)
                    .HasForeignKey(bu => bu.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(bu => bu.BlockedUserEntity)
                    .WithMany(u => u.BlockedByUsers)
                    .HasForeignKey(bu => bu.BlockedUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Bot configuration
            modelBuilder.Entity<Bot>(entity =>
            {
                entity.ToTable("bots");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql(uuidGenerator);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql(timestampGenerator);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql(timestampGenerator);
            });

            // BotChat configuration
            modelBuilder.Entity<BotChat>(entity =>
            {
                entity.ToTable("bot_chats");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql(timestampGenerator);

                entity.HasOne(bc => bc.Bot)
                    .WithMany(b => b.BotChats)
                    .HasForeignKey(bc => bc.BotId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(bc => bc.User)
                    .WithMany(u => u.BotChats)
                    .HasForeignKey(bc => bc.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}