using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ChitChatApp.Core.Infrastructure.Persistence;
using ChitChatApp.Core.Contracts.Repositories;
using ChitChatApp.Core.Infrastructure.Repositories;
using ChitChatApp.Api.Configuration;
using ChitChatApp.Api.Hubs;
using ChitChatApp.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// =====================================
// CONFIGURE SERVICES
// =====================================

// Add controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization for better API responses
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true; // Pretty print in development
    });

// Configure Entity Framework with SQLite for development, PostgreSQL for production
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    // Use SQLite for development (easier local setup), PostgreSQL for production
    if (builder.Environment.IsDevelopment() && connectionString!.StartsWith("Data Source="))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
    
    // Enable sensitive data logging in development for debugging
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Configure JWT settings
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
if (jwtSettings == null)
{
    throw new InvalidOperationException("JWT settings are not configured properly.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // Allow HTTP in development
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero // Remove default 5-minute tolerance for token expiration
    };
    
    // Configure JWT for SignalR connections
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Allow SignalR to receive JWT token from query string
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            if (builder.Environment.IsDevelopment())
            {
                Console.WriteLine($"JWT Authentication failed: {context.Exception.Message}");
            }
            return Task.CompletedTask;
        }
    };
});

// Add authorization
builder.Services.AddAuthorization();

// Register all repositories with dependency injection
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IBlockedUserRepository, BlockedUserRepository>();
builder.Services.AddScoped<IBotRepository, BotRepository>();

// Configure SignalR with optimized settings
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15); // Send keep-alive pings every 15 seconds
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30); // Consider client disconnected after 30 seconds
    options.HandshakeTimeout = TimeSpan.FromSeconds(15); // Timeout for initial handshake
    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB max message size
});

// Add CORS for client applications
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowChitChatClients", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",    // React development server
                "https://localhost:3001",   // Blazor development server
                "http://localhost:5000",    // Avalonia development
                "https://localhost:5001"    // Avalonia development HTTPS
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR
    });
});

// Configure Swagger/OpenAPI for development
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ChitChatApp API",
        Version = "v1",
        Description = "A comprehensive chat application API with real-time messaging, user management, and bot interactions."
    });
    
    // Configure JWT authentication in Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add health checks
var healthChecksBuilder = builder.Services.AddHealthChecks();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

// Add appropriate health check based on database provider
if (builder.Environment.IsDevelopment() && connectionString.StartsWith("Data Source="))
{
    // For SQLite, we'll just add a basic check (no specific package needed)
    healthChecksBuilder.AddCheck("database", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("SQLite database"));
}
else
{
    // For PostgreSQL
    healthChecksBuilder.AddNpgSql(connectionString, name: "database");
}

var app = builder.Build();

// =====================================
// CONFIGURE REQUEST PIPELINE
// =====================================

// Initialize database on startup
await DatabaseInitializer.InitializeDatabaseAsync(app);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ChitChatApp API v1");
        options.RoutePrefix = "swagger"; // Access Swagger at /swagger
    });
}

// Enable CORS
app.UseCors("AllowChitChatClients");

// Redirect HTTP to HTTPS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map SignalR hub
app.MapHub<ChatHub>("/chathub");

// Map health checks
app.MapHealthChecks("/health");

// Add a simple root endpoint for API status
app.MapGet("/", () => new
{
    Application = "ChitChatApp API",
    Version = "1.0.0",
    Status = "Running",
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName
});

// Start the application
Console.WriteLine("🚀 ChitChatApp API is starting...");
Console.WriteLine($"📊 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"🔐 JWT Issuer: {jwtSettings.Issuer}");

if (app.Environment.IsDevelopment())
{
    Console.WriteLine("📖 Swagger UI available at: /swagger");
    Console.WriteLine("💬 SignalR Hub available at: /chathub");
    Console.WriteLine("❤️  Health checks available at: /health");
}

app.Run();