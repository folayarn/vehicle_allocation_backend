using Vehicle_Information_System.Dtos;
using Vehicle_Information_System.Models;
using Vehicle_Information_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Vehicle_Information_System.Controllers
{
    [Authorize]
    [Route("api")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly TokenService _generateToken;
        private readonly UserService _userService;
        public readonly ILogger<UserController> _logger;

        public UserController(
            ApplicationDbContext context,
            TokenService generateToken,
            UserService userService,
            ILogger<UserController> logger)
        {
            _context = context;
            _generateToken = generateToken;
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("user/{id}")]
        public async Task<IActionResult> FetchUser([FromRoute] string id)
        {
            if (!Guid.TryParse(id, out var userId) || userId == Guid.Empty)
            {
                return BadRequest(new { message = "Invalid user ID format" });
            }

            try
            {
                // Search across all user types
                var fleetUser = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => new { u.Fullname, u.Command, u.Email, u.Rank, u.Phone, u.Status, Type = "Fleet" })
                    .FirstOrDefaultAsync();

                if (fleetUser != null)
                    return Ok(new { data = fleetUser });

                var assetUser = await _context.AssetUsers
                    .Where(u => u.UserId == userId)
                    .Select(u => new { u.Fullname, u.Command, u.Email, u.Rank, u.Phone, u.Status, Type = "Asset" })
                    .FirstOrDefaultAsync();

                if (assetUser != null)
                    return Ok(new { data = assetUser });

                var accommodationUser = await _context.AccomodationUsers
                    .Where(u => u.UserId == userId)
                    .Select(u => new { u.Fullname, u.Command, u.Email, u.Rank, u.Phone, u.Status, Type = "Accommodation" })
                    .FirstOrDefaultAsync();

                if (accommodationUser != null)
                    return Ok(new { data = accommodationUser });

                var storeUser = await _context.StoreUsers
                    .Where(u => u.UserId == userId)
                    .Select(u => new { u.Fullname, u.Command, u.Email, u.Rank, u.Phone, u.Status, Type = "Store" })
                    .FirstOrDefaultAsync();

                if (storeUser != null)
                    return Ok(new { data = storeUser });

                return NotFound(new { message = "User not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user with ID {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDto userDto)
        {
            if (userDto == null)
            {
                return BadRequest(new { message = "User data is required" });
            }

            try
            {
                if (string.IsNullOrEmpty(userDto.Email) || string.IsNullOrEmpty(userDto.Password))
                {
                    return BadRequest(new { message = "Email and password are required" });
                }

                var validUserTypes = new[] { "Fleet", "Asset", "Accommodation", "Store" };
                if (string.IsNullOrEmpty(userDto.UserType) || !validUserTypes.Contains(userDto.UserType))
                {
                    return BadRequest(new { message = "Invalid UserType. Must be Fleet, Asset, Accommodation, or Store" });
                }

                // Check if user already exists across all tables
                var userExists = await CheckUserExistsByEmail(userDto.Email);
                if (userExists)
                {
                    return Conflict(new { message = "User already exists" });
                }

                string hashedPassword = _generateToken.HashPassword(userDto.Password);

                // Create user based on type
                switch (userDto.UserType)
                {
                    case "Fleet":
                        var fleetUser = new User
                        {
                            Email = userDto.Email.Trim().ToLower(),
                            Command = userDto.Command,
                            Svn = userDto.Svn?.Trim(),
                            Zone = userDto.Zone,
                            Fullname = userDto.Fullname?.Trim(),
                            Rank = userDto.Rank?.Trim(),
                            AccessLevel = userDto.AccessLevel?.ToLower(),
                            Phone = userDto.Phone?.Trim(),
                            Password = hashedPassword,
                            DateCreated = DateTime.UtcNow,
                            Status = "Active"
                        };
                        await _context.Users.AddAsync(fleetUser);
                        break;

                    case "Accommodation":
                        var accommodationUser = new AccomodationUser
                        {
                            Email = userDto.Email.Trim().ToLower(),
                            Command = userDto.Command,
                            Svn = userDto.Svn?.Trim(),
                            Zone = userDto.Zone,
                            Fullname = userDto.Fullname?.Trim(),
                            Rank = userDto.Rank?.Trim(),
                            AccessLevel = userDto.AccessLevel?.ToLower(),
                            Phone = userDto.Phone?.Trim(),
                            Password = hashedPassword,
                            DateCreated = DateTime.UtcNow,
                            Status = "Active"
                        };
                        await _context.AccomodationUsers.AddAsync(accommodationUser);
                        break;

                    case "Asset":
                        var assetUser = new AssetUser
                        {
                            Email = userDto.Email.Trim().ToLower(),
                            Command = userDto.Command,
                            Svn = userDto.Svn?.Trim(),
                            Zone = userDto.Zone,
                            Fullname = userDto.Fullname?.Trim(),
                            Rank = userDto.Rank?.Trim(),
                            AccessLevel = userDto.AccessLevel?.ToLower(),
                            Phone = userDto.Phone?.Trim(),
                            Password = hashedPassword,
                            DateCreated = DateTime.UtcNow,
                            Status = "Active"
                        };
                        await _context.AssetUsers.AddAsync(assetUser);
                        break;

                    default: // Store
                        var storeUser = new StoreUser
                        {
                            Email = userDto.Email.Trim().ToLower(),
                            Command = userDto.Command,
                            Svn = userDto.Svn?.Trim(),
                            Zone = userDto.Zone,
                            Fullname = userDto.Fullname?.Trim(),
                            Rank = userDto.Rank?.Trim(),
                            AccessLevel = userDto.AccessLevel?.ToLower(),
                            Phone = userDto.Phone?.Trim(),
                            Password = hashedPassword,
                            DateCreated = DateTime.UtcNow,
                            Status = "Active"
                        };
                        await _context.StoreUsers.AddAsync(storeUser);
                        break;
                }

                await _context.SaveChangesAsync();

                // Send welcome email (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var fullname = userDto.Fullname ?? "User";
                        var subject = "Welcome to Vehicle Information System";
                        var body = $@"
                            <html>
                            <body>
                                <h2>Welcome {fullname}!</h2>
                                <p>Your account has been created successfully.</p>
                                <p><strong>Default Password:</strong> {userDto.Password}</p>
                                <p><strong>Email:</strong> {userDto.Email}</p>
                                <p><strong>User Type:</strong> {userDto.UserType}</p>
                                <p>Please log in and change your password immediately.</p>
                                <hr>
                                <p>Best regards,<br>Vehicle Information System Team</p>
                            </body>
                            </html>
                        ";

                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send welcome email to {Email}", userDto.Email);
                    }
                });

                _logger.LogInformation("User registered successfully: {Email} ({UserType})", userDto.Email, userDto.UserType);

                return Ok(new { message = "User successfully registered", email = userDto.Email, userType = userDto.UserType });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error during user registration for {Email}", userDto?.Email);
                return StatusCode(500, new { message = "Database error occurred while creating user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during user registration for {Email}", userDto?.Email);
                return StatusCode(500, new { message = "An error occurred while processing your request" });
            }
        }

        [HttpPut("edit-user")]
        public async Task<IActionResult> EditUser([FromBody] EditUserDto userDto)
        {
            if (userDto == null || userDto.UserId == Guid.Empty)
                return BadRequest(new { message = "Invalid user data. UserId is required." });

            try
            {
                // Search across all user types
                var user = await FindUserById(userDto.UserId);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                // Update common properties
                if (!string.IsNullOrEmpty(userDto.Command))
                    user.Command = userDto.Command;

                if (!string.IsNullOrEmpty(userDto.Fullname))
                    user.Fullname = userDto.Fullname;

                if (!string.IsNullOrEmpty(userDto.Rank))
                    user.Rank = userDto.Rank;

                if (!string.IsNullOrEmpty(userDto.Svn))
                    user.Svn = userDto.Svn;

                if (!string.IsNullOrEmpty(userDto.Phone))
                    user.Phone = userDto.Phone;

                if (!string.IsNullOrEmpty(userDto.AccessLevel))
                    user.AccessLevel = userDto.AccessLevel;

                if (!string.IsNullOrEmpty(userDto.Email))
                    user.Email = userDto.Email;

                // Update based on type
                switch (user.UserType)
                {
                    case "Fleet":
                        _context.Users.Update((User)user);
                        break;
                    case "Asset":
                        _context.AssetUsers.Update((AssetUser)user);
                        break;
                    case "Accommodation":
                        _context.AccomodationUsers.Update((AccomodationUser)user);
                        break;
                    case "Store":
                        _context.StoreUsers.Update((StoreUser)user);
                        break;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "User updated successfully." });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict updating user {UserId}", userDto.UserId);
                return Conflict(new { message = "The record was modified by another user. Please refresh and try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating user");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPut("suspend-user/{userId}")]
        public async Task<IActionResult> SuspendUser(Guid userId)
        {
            try
            {
                var user = await FindUserById(userId);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                user.Status = "Suspended";

                await UpdateUserInContext(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "User successfully suspended." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending user {UserId}", userId);
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        [HttpPut("unsuspend-user/{userId}")]
        public async Task<IActionResult> UnSuspendUser(Guid userId)
        {
            try
            {
                var user = await FindUserById(userId);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                user.Status = "Active";

                await UpdateUserInContext(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "User successfully activated." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsuspending user {UserId}", userId);
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        [HttpDelete("delete-user/{userId}")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            try
            {
                var user = await FindUserById(userId);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                await RemoveUserFromContext(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "User successfully deleted." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        [HttpPut("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] PasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid data." });

            try
            {
                var user = await FindUserById(request.OfficerId);
                if (user == null)
                    return Unauthorized(new { message = "User not found." });

                // Check if the old password matches
                if (user.Password != _generateToken.HashPassword(request.OldPassword))
                    return BadRequest(new { message = "Incorrect old password." });

                // Validate new password strength
                if (!ValidatePassword(request.NewPassword))
                    return BadRequest(new
                    {
                        message = "Password should be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, one number, and one special character."
                    });

                user.Password = _generateToken.HashPassword(request.NewPassword);

                await UpdateUserInContext(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Password updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password for user {UserId}", request.OfficerId);
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        [HttpPut("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid data." });

            try
            {
                var user = await FindUserById(request.OfficerId);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                if (!ValidatePassword(request.NewPassword))
                    return BadRequest(new
                    {
                        message = "Password should be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, one number, and one special character."
                    });

                user.Password = _generateToken.HashPassword(request.NewPassword);

                await UpdateUserInContext(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Password updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", request.OfficerId);
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                // Combine all user types
                var fleetUsers = await _context.Users
                    .Where(p => p.Status == "Active" || p.Status == "Suspended")
                    .Select(r => new
                    {
                        r.UserId,
                        r.Email,
                        r.Command,
                        r.Zone,
                        r.Svn,
                        r.AccessLevel,
                        r.Fullname,
                        r.Rank,
                        r.Status,
                        r.Phone,
                        r.DateCreated,
                        UserType = "Fleet"
                    }).ToListAsync();

                var assetUsers = await _context.AssetUsers
                    .Where(p => p.Status == "Active" || p.Status == "Suspended")
                    .Select(r => new
                    {
                        r.UserId,
                        r.Email,
                        r.Command,
                        r.Zone,
                        r.Svn,
                        r.AccessLevel,
                        r.Fullname,
                        r.Rank,
                        r.Status,
                        r.Phone,
                        r.DateCreated,
                        UserType = "Asset"
                    }).ToListAsync();

                var accommodationUsers = await _context.AccomodationUsers
                    .Where(p => p.Status == "Active" || p.Status == "Suspended")
                    .Select(r => new
                    {
                        r.UserId,
                        r.Email,
                        r.Command,
                        r.Zone,
                        r.Svn,
                        r.AccessLevel,
                        r.Fullname,
                        r.Rank,
                        r.Status,
                        r.Phone,
                        r.DateCreated,
                        UserType = "Accommodation"
                    }).ToListAsync();

                var storeUsers = await _context.StoreUsers
                    .Where(p => p.Status == "Active" || p.Status == "Suspended")
                    .Select(r => new
                    {
                        r.UserId,
                        r.Email,
                        r.Command,
                        r.Zone,
                        r.Svn,
                        r.AccessLevel,
                        r.Fullname,
                        r.Rank,
                        r.Status,
                        r.Phone,
                        r.DateCreated,
                        UserType = "Store"
                    }).ToListAsync();

                var allUsers = fleetUsers.Concat(assetUsers)
                    .Concat(accommodationUsers)
                    .Concat(storeUsers)
                    .OrderByDescending(r => r.DateCreated)
                    .ToList();

                return Ok(new { data = allUsers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Helper Methods
        private async Task<dynamic> FindUserById(Guid userId)
        {
            var fleetUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (fleetUser != null)
            {
                return fleetUser;
            }

            var assetUser = await _context.AssetUsers.FirstOrDefaultAsync(u => u.UserId == userId);
            if (assetUser != null)
            {
                return assetUser;
            }

            var accommodationUser = await _context.AccomodationUsers.FirstOrDefaultAsync(u => u.UserId == userId);
            if (accommodationUser != null)
            {
                return accommodationUser;
            }

            var storeUser = await _context.StoreUsers.FirstOrDefaultAsync(u => u.UserId == userId);
            if (storeUser != null)
            {
                
                return storeUser;
            }

            return null;
        }

        private async Task<bool> CheckUserExistsByEmail(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email) ||
                   await _context.AssetUsers.AnyAsync(u => u.Email == email) ||
                   await _context.AccomodationUsers.AnyAsync(u => u.Email == email) ||
                   await _context.StoreUsers.AnyAsync(u => u.Email == email);
        }

        private async Task UpdateUserInContext(dynamic user)
        {
            switch (user.UserType)
            {
                case "Fleet":
                    _context.Users.Update(user);
                    break;
                case "Asset":
                    _context.AssetUsers.Update(user);
                    break;
                case "Accommodation":
                    _context.AccomodationUsers.Update(user);
                    break;
                case "Store":
                    _context.StoreUsers.Update(user);
                    break;
            }
            await Task.CompletedTask;
        }

        private async Task RemoveUserFromContext(dynamic user)
        {
            switch (user.UserType)
            {
                case "Fleet":
                    _context.Users.Remove(user);
                    break;
                case "Asset":
                    _context.AssetUsers.Remove(user);
                    break;
                case "Accommodation":
                    _context.AccomodationUsers.Remove(user);
                    break;
                case "Store":
                    _context.StoreUsers.Remove(user);
                    break;
            }
            await Task.CompletedTask;
        }

        private bool ValidatePassword(string password)
        {
            return password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => !char.IsLetterOrDigit(ch));
        }
    }
}

// Add this DTO
public class EditUserDto
{
    public Guid UserId { get; set; }
    public string? Command { get; set; }
    public string? Fullname { get; set; }
    public string? Rank { get; set; }
    public string? Svn { get; set; }
    public string? Phone { get; set; }
    public string? AccessLevel { get; set; }
    public string? Email { get; set; }
}