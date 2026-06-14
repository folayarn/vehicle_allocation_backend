using DocumentFormat.OpenXml.Bibliography;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
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
        policy.WithOrigins(["http://localhost:5173","https://fms.customs.gov.ng"])
              .AllowAnyHeader()
              .AllowCredentials()
              .AllowAnyMethod();
    });
});

var connectionString = $"Host={localhost};Database={database};Username={username};Password={password};";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add custom services
builder.Services.AddHostedService<MaintenanceBackgroundService>();

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddHostedService<MaintenanceBackgroundService>();


// Add controllers and Swagger
// For .NET 6/7/8 with Program.cs
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        // Or use Preserve for more control:
        // options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
    });
//builder.Services.AddControllers();
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


app.UseCors("AllowAllOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();