using DocumentFormat.OpenXml.Bibliography;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Serilog;
using Serilog.Events;
using System.Data.Common;
using System.Text;
using System.Text.Json.Serialization;
using Vehicle_Information_System.Services;

// Load .env file
Env.Load();
Env.TraversePath().Load();

// Get values with null checks
var localhost = Env.GetString("DATABASE_HOST");
var database = Env.GetString("DATABASE");
var password = Env.GetString("DATABASE_PASSWORD");
var username = Env.GetString("DATABASE_USERNAME");
var secret = Env.GetString("Secret");

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "VehicleInformationSystem")
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
    )
    .WriteTo.File(
        path: "Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 31,
        fileSizeLimitBytes: 10485760, // 10MB
        rollOnFileSizeLimit: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
    )
    .WriteTo.File(
        path: "Logs/log-structured-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 31,
        formatter: new Serilog.Formatting.Compact.CompactJsonFormatter()
    )
    .CreateLogger();

try
{
    Log.Information("Starting Vehicle Information System API");
    Log.Information("Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog to the builder
    builder.Host.UseSerilog();

    // DEBUG: Check if secret is loaded
    Log.Debug("Secret loaded: {SecretLoaded}", string.IsNullOrEmpty(secret) ? "NO - SECRET IS NULL!" : "YES");
    if (!string.IsNullOrEmpty(secret))
    {
        Log.Debug("Secret length: {SecretLength}", secret.Length);
        Log.Debug("Secret first 10 chars: {SecretFirstChars}", secret.Substring(0, Math.Min(10, secret.Length)));
    }
    else
    {
        Log.Error("ERROR: Secret is NULL! Check your .env file location and content");
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
                Log.Warning(context.Exception, "Authentication failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Log.Debug("Token validated successfully for user: {User}", context.Principal?.Identity?.Name ?? "Unknown");
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
    builder.Services.AddScoped<LogService>(); // Add log service

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
            Log.Information("Testing database connection...");
            var canConnect = await dbContext.Database.CanConnectAsync();
            if (canConnect)
            {
                Log.Information("✓ Database connection successful!");
            }
            else
            {
                Log.Warning("✗ Database connection failed!");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "✗ Database connection error");
        }
    }

    app.UseCors("AllowAllOrigins");
    app.UseAuthentication();
    app.UseAuthorization();

    // Add logging middleware
    app.Use(async (context, next) =>
    {
        var startTime = DateTime.UtcNow;
        var requestPath = context.Request.Path;
        var method = context.Request.Method;

        // Log the request
        Log.Information("Request: {Method} {Path} started", method, requestPath);

        try
        {
            await next();

            var elapsed = DateTime.UtcNow - startTime;
            var statusCode = context.Response.StatusCode;

            if (statusCode >= 400)
            {
                Log.Warning("Request: {Method} {Path} completed with {StatusCode} in {Elapsed}ms",
                    method, requestPath, statusCode, elapsed.TotalMilliseconds);
            }
            else
            {
                Log.Information("Request: {Method} {Path} completed with {StatusCode} in {Elapsed}ms",
                    method, requestPath, statusCode, elapsed.TotalMilliseconds);
            }
        }
        catch (Exception ex)
        {
            var elapsed = DateTime.UtcNow - startTime;
            Log.Error(ex, "Request: {Method} {Path} failed after {Elapsed}ms", method, requestPath, elapsed.TotalMilliseconds);
            throw;
        }
    });

    app.MapControllers();

    Log.Information("Application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;

// Custom interceptor for logging database connections
public class NpgsqlConnectionInterceptor : DbCommandInterceptor
{
    private readonly ILogger<NpgsqlConnectionInterceptor>? _logger;

    public NpgsqlConnectionInterceptor()
    {
        // No logger available in constructor, use static Serilog
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        if (command.CommandTimeout < 30)
        {
            command.CommandTimeout = 60; // Ensure minimum timeout
        }

        Log.Debug("Executing SQL: {CommandText}", command.CommandText);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandTimeout < 30)
        {
            command.CommandTimeout = 60;
        }

        Log.Debug("Executing SQL (async): {CommandText}", command.CommandText);
        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        Log.Debug("Executing non-query SQL: {CommandText}", command.CommandText);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Log.Debug("Executing non-query SQL (async): {CommandText}", command.CommandText);
        return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }
}