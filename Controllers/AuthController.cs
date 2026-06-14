using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Vehicle_Information_System.Dtos;
using Vehicle_Information_System.Models;
using Vehicle_Information_System.Services;

namespace Vehicle_Information_System.Controllers
{
    [Route("api")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly TokenService _generateToken;
        private readonly IConfiguration _configuration;
        private readonly UserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            IConfiguration configuration,
            UserService userService,
            TokenService generateToken,
            ILogger<AuthController> logger)
        {
            _userService = userService;
            _configuration = configuration;
            _context = context;
            _generateToken = generateToken;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var message = new { Text = "Welcome home!! API is working" };
            return Ok(message);
        }

        public class OtpVerificationDto
        {
            public string Email { get; set; }
            public string Password { get; set; }
            public string UserType { get; set; } = "Fleet";
        }

        public class RefreshTokenRequest
        {
            public string RefreshToken { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] OtpVerificationDto otpVerification)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(otpVerification.Email) || string.IsNullOrEmpty(otpVerification.Password))
                {
                    return BadRequest(new { message = "Email and password are required" });
                }

                // Define variables
                dynamic user = null;
                string userType = otpVerification.UserType;
                Guid Id = Guid.Empty;
                string accessLevel = null;
                string status = null;
                string storedPassword = null;
                string fullname = null;
                string email = otpVerification.Email;

                // Fetch user based on type
                switch (otpVerification.UserType?.ToLower())
                {
                    case "fleet":
                        var fleetUser = await _context.Users
                            .FirstOrDefaultAsync(u => u.Email == otpVerification.Email);
                        if (fleetUser != null)
                        {
                            user = fleetUser;
                            Id = fleetUser.UserId;
                            accessLevel = fleetUser.AccessLevel;
                            status = fleetUser.Status;
                            storedPassword = fleetUser.Password;
                            fullname = fleetUser.Fullname;
                            userType = "Fleet";
                        }
                        break;

                    case "store":
                        var storeUser = await _context.StoreUsers
                            .FirstOrDefaultAsync(u => u.Email == otpVerification.Email);
                        if (storeUser != null)
                        {
                            user = storeUser;
                            Id = storeUser.UserId;
                            accessLevel = storeUser.AccessLevel;
                            status = storeUser.Status;
                            storedPassword = storeUser.Password;
                            fullname = storeUser.Fullname;
                            userType = "Store";
                        }
                        break;

                    case "accommodation":
                        var accommodationUser = await _context.AccomodationUsers
                            .FirstOrDefaultAsync(u => u.Email == otpVerification.Email);
                        if (accommodationUser != null)
                        {
                            user = accommodationUser;
                            Id = accommodationUser.UserId;
                            accessLevel = accommodationUser.AccessLevel;
                            status = accommodationUser.Status;
                            storedPassword = accommodationUser.Password;
                            fullname = accommodationUser.Fullname;
                            userType = "Accommodation";
                        }
                        break;

                    case "asset":
                        var assetUser = await _context.AssetUsers
                            .FirstOrDefaultAsync(u => u.Email == otpVerification.Email);
                        if (assetUser != null)
                        {
                            user = assetUser;
                            Id = assetUser.UserId;
                            accessLevel = assetUser.AccessLevel;
                            status = assetUser.Status;
                            storedPassword = assetUser.Password;
                            fullname = assetUser.Fullname;
                            userType = "Asset";
                        }
                        break;

                    default:
                        return BadRequest(new { message = "Invalid user type specified. Must be Fleet, Store, Accommodation, or Asset" });
                }

                // Check if user exists
                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed: User not found for email {Email}", otpVerification.Email);
                    return BadRequest(new { message = "Invalid email or password" });
                }

                // Check account status
                if (status != "Active")
                {
                    _logger.LogWarning("Login attempt failed: Account {Email} is {Status}", otpVerification.Email, status);
                    return Unauthorized(new { message = $"Account is {status?.ToLower()}. Please contact support." });
                }

                // Verify password
                if (!VerifyPassword(otpVerification.Password, storedPassword))
                {
                    _logger.LogWarning("Login attempt failed: Invalid password for {Email}", otpVerification.Email);
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Generate tokens - FIXED: Don't try to deconstruct dynamic
                string accessToken;
                string refreshToken;

                if (userType == "Fleet")
                {
                    var tokens = _generateToken.GenerateNewToken((User)user);
                    accessToken = tokens.AccessToken;
                    refreshToken = tokens.RefreshToken;
                }
                else if (userType == "Store")
                {
                    var tokens = _generateToken.GenerateStoreToken((StoreUser)user);
                    accessToken = tokens.AccessToken;
                    refreshToken = tokens.RefreshToken;
                }
                else if (userType == "Accommodation")
                {
                    var tokens = _generateToken.GenerateAccommodationToken((AccomodationUser)user);
                    accessToken = tokens.AccessToken;
                    refreshToken = tokens.RefreshToken;
                }
                else if (userType == "Asset")
                {
                    var tokens = _generateToken.GenerateAssetToken((AssetUser)user);
                    accessToken = tokens.AccessToken;
                    refreshToken = tokens.RefreshToken;
                }
                else
                {
                    return BadRequest(new { message = "Invalid user type" });
                }

                // Save refresh token to database
                await SaveRefreshToken(user, userType, refreshToken);

                _logger.LogInformation("User logged in successfully: {Email} ({UserType})", otpVerification.Email, userType);

                return Ok(new
                {
                    message = "Login successful",
                    user_token = accessToken,
                    refresh_token = refreshToken,
                    user_type = userType,
                    user_access_level = accessLevel,
                    id = Id,
                    email = otpVerification.Email,
                    fullname = fullname
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email {Email}", otpVerification.Email);
                return StatusCode(500, new { message = $"An error occurred during login. Please try again later. {ex.StackTrace}" });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token is required" });
            }

            try
            {
                // Search for refresh token across all user tables
                var (user, userType) = await GetUserByRefreshToken(request.RefreshToken);

                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid refresh token" });
                }

                var refreshTokenExpiry = GetRefreshTokenExpiry(user, userType);
                if (refreshTokenExpiry <= DateTime.UtcNow)
                {
                    return Unauthorized(new { message = "Refresh token has expired" });
                }

                // Generate new tokens
                var (accessToken, refreshToken) = GenerateTokenForUserType(user, userType);

                // Update refresh token in database
                await UpdateRefreshToken(user, userType, refreshToken);

                _logger.LogInformation("Token refreshed successfully for user type {UserType}", userType);

                return Ok(new
                {
                    access_token = accessToken,
                    refresh_token = refreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { message = "An error occurred while refreshing token" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Get user email from claims
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                var userType = User.FindFirst("UserType")?.Value;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(userType))
                {
                    return BadRequest(new { message = "Unable to identify user" });
                }

                // Clear refresh token
                await ClearRefreshToken(email, userType);

                _logger.LogInformation("User logged out: {Email}", email);
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "An error occurred during logout" });
            }
        }

        // Helper Methods

        private (string AccessToken, string RefreshToken) GenerateTokenForUserType(object user, string userType)
        {
            return userType switch
            {
                "Fleet" => _generateToken.GenerateNewToken((User)user),
                "Store" => _generateToken.GenerateStoreToken((StoreUser)user),
                "Accommodation" => _generateToken.GenerateAccommodationToken((AccomodationUser)user),
                "Asset" => _generateToken.GenerateAssetToken((AssetUser)user),
                _ => throw new ArgumentException($"Invalid user type: {userType}")
            };
        }

        private async Task SaveRefreshToken(dynamic user, string userType, string refreshToken)
        {
            switch (userType)
            {
                case "Fleet":
                    user.RefreshToken = refreshToken;
                    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                    _context.Users.Update(user);
                    break;
                case "Store":
                    user.RefreshToken = refreshToken;
                    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                    _context.StoreUsers.Update(user);
                    break;
                case "Accommodation":
                    user.RefreshToken = refreshToken;
                    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                    _context.AccomodationUsers.Update(user);
                    break;
                case "Asset":
                    user.RefreshToken = refreshToken;
                    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                    _context.AssetUsers.Update(user);
                    break;
            }
            await _context.SaveChangesAsync();
        }

        private async Task UpdateRefreshToken(dynamic user, string userType, string refreshToken)
        {
            switch (userType)
            {
                case "Fleet":
                    user.RefreshToken = refreshToken;
                    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                    _context.Users.Update(user);
                    break;
                case "Store":
                    user.RefreshToken = refreshToken;
                    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                    _context.StoreUsers.Update(user);
                    break;
                case "Accommodation":
                    user.RefreshToken = refreshToken;
                    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                    _context.AccomodationUsers.Update(user);
                    break;
                case "Asset":
                    user.RefreshToken = refreshToken;
                    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                    _context.AssetUsers.Update(user);
                    break;
            }
            await _context.SaveChangesAsync();
        }

        private async Task ClearRefreshToken(string email, string userType)
        {
            switch (userType)
            {
                case "Fleet":
                    var fleetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                    if (fleetUser != null)
                    {
                        fleetUser.RefreshToken = null;
                        fleetUser.RefreshTokenExpiryTime = null;
                        await _context.SaveChangesAsync();
                    }
                    break;
                case "Store":
                    var storeUser = await _context.StoreUsers.FirstOrDefaultAsync(u => u.Email == email);
                    if (storeUser != null)
                    {
                        storeUser.RefreshToken = null;
                        storeUser.RefreshTokenExpiryTime = null;
                        await _context.SaveChangesAsync();
                    }
                    break;
                case "Accommodation":
                    var accommodationUser = await _context.AccomodationUsers.FirstOrDefaultAsync(u => u.Email == email);
                    if (accommodationUser != null)
                    {
                        accommodationUser.RefreshToken = null;
                        accommodationUser.RefreshTokenExpiryTime = null;
                        await _context.SaveChangesAsync();
                    }
                    break;
                case "Asset":
                    var assetUser = await _context.AssetUsers.FirstOrDefaultAsync(u => u.Email == email);
                    if (assetUser != null)
                    {
                        assetUser.RefreshToken = null;
                        assetUser.RefreshTokenExpiryTime = null;
                        await _context.SaveChangesAsync();
                    }
                    break;
            }
        }

        private async Task<(object User, string UserType)> GetUserByRefreshToken(string refreshToken)
        {
            // Check Fleet users
            var fleetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
            if (fleetUser != null)
                return (fleetUser, "Fleet");

            // Check Store users
            var storeUser = await _context.StoreUsers
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
            if (storeUser != null)
                return (storeUser, "Store");

            // Check Accommodation users
            var accommodationUser = await _context.AccomodationUsers
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
            if (accommodationUser != null)
                return (accommodationUser, "Accommodation");

            // Check Asset users
            var assetUser = await _context.AssetUsers
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
            if (assetUser != null)
                return (assetUser, "Asset");

            return (null, null);
        }

        private DateTime GetRefreshTokenExpiry(dynamic user, string userType)
        {
            return userType switch
            {
                "Fleet" => ((User)user).RefreshTokenExpiryTime ?? DateTime.UtcNow,
                "Store" => ((StoreUser)user).RefreshTokenExpiryTime ?? DateTime.UtcNow,
                "Accommodation" => ((AccomodationUser)user).RefreshTokenExpiryTime ?? DateTime.UtcNow,
                "Asset" => ((AssetUser)user).RefreshTokenExpiryTime ?? DateTime.UtcNow,
                _ => DateTime.UtcNow
            };
        }

        private string GetUserFullName(dynamic user, string userType)
        {
            return userType switch
            {
                "Fleet" => ((User)user).Fullname,
                "Store" => ((StoreUser)user).Fullname,
                "Accommodation" => ((AccomodationUser)user).Fullname,
                "Asset" => ((AssetUser)user).Fullname,
                _ => null
            };
        }

        private bool VerifyPassword(string inputPassword, string storedHashedPassword)
        {
            if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedHashedPassword))
                return false;

            var hashInputPassword = _generateToken.HashPassword(inputPassword);
            return hashInputPassword == storedHashedPassword;
        }
    }
}