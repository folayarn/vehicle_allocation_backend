using DocumentFormat.OpenXml.Bibliography;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Data.Common;
using System.Text;
using System.Text.Json.Serialization;
using Vehicle_Information_System.Services;

// Load .env file
Env.Load();
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Get values with null checks
var localhost = Env.GetString("DATABASE_HOST");
var database = Env.GetString("DATABASE");
var password = Env.GetString("DATABASE_PASSWORD");
var username = Env.GetString("DATABASE_USERNAME");
var secret = Env.GetString("Secret");

// DEBUG: Check if secret is loaded
Console.WriteLine($"Secret loaded: {(string.IsNullOrEmpty(secret) ? "NO - SECRET IS NULL!" : "YES")}");
if (!string.IsNullOrEmpty(secret))
{
    Console.WriteLine($"Secret length: {secret.Length}");
    Console.WriteLine($"Secret first 10 chars: {secret.Substring(0, Math.Min(10, secret.Length))}");
}
else
{
    Console.WriteLine("ERROR: Secret is NULL! Check your .env file location and content");
}

// CRITICAL: Check if secret is null and throw clear error
if (string.IsNullOrEmpty(secret))
{
    throw new Exception("JWT Secret is not configured in .env file. Please add Secret=YourSecretKey to .env");
}

var jwtSettings = builder.Configuration.GetSection("Jwt");

var issuer = jwtSettings["Issuer"] ?? "VehicleApi";
var audience = jwtSettings["Audience"] ?? "VehicleApi";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ClockSkew = TimeSpan.Zero
    };

    // Add this to debug JWT issues
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token validated successfully");
            return Task.CompletedTask;
        }
    };
});

// Configure CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.WithOrigins(["http://localhost:5173", "https://fms.customs.gov.ng"])
              .AllowAnyHeader()
              .AllowCredentials()
              .AllowAnyMethod();
    });
});

// Build connection string with proper timeout and pooling settings
var connectionString = $"Host={localhost};Database={database};Username={username};Password={password};" +
    "Pooling=true;" +
    "Maximum Pool Size=20;" +
    "Minimum Pool Size=2;" +
    "Connection Idle Lifetime=300;" +
    "Connection Lifetime=0;" +
    "Timeout=30;" +           // Connection timeout (seconds)
    "Command Timeout=60;" +    // Command timeout (seconds)
    "Keepalive=300;" +         // Send keepalive every 5 minutes
    "Include Error Detail=true;" + // Helpful for debugging
    "Trust Server Certificate=true;"; // Add if using self-signed certs

// Configure DbContext with retry logic and resilience
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Enable automatic retry on failure
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: new[] { "57P01", "57P02", "57P03", "08000", "08003", "08006", "08007" });

        // Set command timeout
        npgsqlOptions.CommandTimeout(60);

        // Enable connection multiplexing (better performance)
    });

    // Enable sensitive data logging only in development
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());

    // Add connection interceptor for logging
    options.AddInterceptors(new NpgsqlConnectionInterceptor());
});

// Add custom services
builder.Services.AddHostedService<MaintenanceBackgroundService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<UserService>();


// Add controllers and Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// IMPORTANT: Configure static files to serve from Uploads folder
app.UseStaticFiles(); // For wwwroot folder

// Add this to serve files from Uploads folder
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Uploads")),
    RequestPath = "/Uploads",
    ServeUnknownFileTypes = true, // Allow serving PDF files
    DefaultContentType = "application/pdf" // Set default content type
});

// Test database connection on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        Console.WriteLine("Testing database connection...");
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (canConnect)
        {
            Console.WriteLine("✓ Database connection successful!");

            // Optional: Run pending migrations on startup
            // var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            // if (pendingMigrations.Any())
            // {
            //     Console.WriteLine($"Applying {pendingMigrations.Count()} pending migrations...");
            //     await dbContext.Database.MigrateAsync();
            //     Console.WriteLine("Migrations applied successfully!");
            // }
        }
        else
        {
            Console.WriteLine("✗ Database connection failed!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Database connection error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        // Don't throw - let the app start but log the error
    }
}

app.UseCors("AllowAllOrigins");
app.UseAuthentication();
app.UseAuthorization();



app.MapControllers();

app.Run();

// Custom interceptor for logging database connections
public class NpgsqlConnectionInterceptor : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        if (command.CommandTimeout < 30)
        {
            command.CommandTimeout = 60; // Ensure minimum timeout
        }
        return base.ReaderExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandTimeout < 30)
        {
            command.CommandTimeout = 60;
        }
        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}