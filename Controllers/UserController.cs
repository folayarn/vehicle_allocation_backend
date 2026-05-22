using Vehicle_Information_System.Dtos;
using Vehicle_Information_System.Models;
using Vehicle_Information_System.Services;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;


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
        public UserController(ApplicationDbContext context, TokenService generateToken
, UserService userService, ILogger<UserController> logger)
        {
            _context = context;
            _generateToken = generateToken;
            _userService = userService;
            _logger = logger;
        }



        [HttpGet("user/{id}")]
        public async Task<IActionResult> FetchUser([FromRoute] string id)
        {
            // Validate input
            if (!Guid.TryParse(id, out var userId) || userId == Guid.Empty)
            {
                return BadRequest(new { message = "Invalid user ID format" });
            }

            try
            {
                // Fetch the user with related factory data in a single query
                var user = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => new
                    {
                        u.Fullname,
                        u.Command,
                        u.Email,
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(new { data = user });
            }
            catch (Exception ex)
            {
                // Log the exception (uncomment when you have a logger)
                // _logger.LogError(ex, "Error fetching user with ID {UserId}", id);

                return StatusCode(500, new
                {
                    message = "An error occurred while processing your request.",
                    detail = ex.Message // Only include in development environment
                });
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
                // Validate input
                if (string.IsNullOrEmpty(userDto.Email) || string.IsNullOrEmpty(userDto.Password))
                {
                    return BadRequest(new { message = "Email and password are required" });
                }

                // Check if the user already exists by email
                var userExists = await _context.Users.AnyAsync(u => u.Email == userDto.Email);
                if (userExists)
                {
                    return Conflict(new { message = "User already exists" }); // 409 is more appropriate
                }

                



                // Map userDto to the User entity
                var user = new User
                {
                    Email = userDto.Email.Trim().ToLower(),
                    Command = userDto.Command,
                    Svn = userDto.Svn?.Trim(),
                    Zone = userDto.Zone,
                   
                    Fullname = userDto.Fullname?.Trim(),
                    Rank = userDto.Rank?.Trim(),
                    AccessLevel = userDto.AccessLevel?.ToLower(),
                  
                    Phone = userDto.Phone?.Trim(),
                    Password = _generateToken.HashPassword(userDto.Password),
                    DateCreated = DateTime.UtcNow,
                };

                // Add user to the database
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                // Send welcome email (fire and forget - don't block registration on email success)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var body = $"Welcome {user.Fullname}! Your account has been created successfully. default Password: {userDto.Password}";
                    }
                    catch (Exception ex)
                    {
                        // Log email failure but don't fail the registration
                        _logger.LogWarning(ex, "Failed to send welcome email to {Email}", user.Email);
                    }
                });

                // Log activity

                return Ok(new { message = "User successfully registered" });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error during user registration");
                return StatusCode(500, new { message = "Database error occurred while creating user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during user registration");
                return StatusCode(500, new { message = "An error occurred while processing your request" });
            }
        }

        [HttpPut("edit-user")]
        public async Task<IActionResult> EditUser([FromBody] UserDto userDto)
        {
            if (userDto == null)
                return BadRequest("Invalid user data.");

            try
            {
               
              

                // 3. Fetch user with explicit tracking
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userDto.OfficerId);

                if (user == null)
                    return NotFound("User not found");

                _logger.LogInformation("Original user data: {@User}", user);

               
                user.Command = userDto.Command;
                user.Fullname = userDto.Fullname ?? user.Fullname;
                user.Rank = userDto.Rank ?? user.Rank;
                user.Svn = userDto.Svn ?? user.Svn;
                
                user.Phone = userDto.Phone ?? user.Phone;
                
                user.AccessLevel = userDto.AccessLevel;
                user.Email = userDto.Email ?? user.Email;


                // 5. Log changes before saving
                var changedProperties = _context.Entry(user)
                    .Properties
                    .Where(p => p.IsModified)
                    .Select(p => new
                    {
                        Property = p.Metadata.Name,
                        Original = p.OriginalValue,
                        Current = p.CurrentValue
                    }).ToList();

                _logger.LogInformation("Detected changes: {@Changes}", changedProperties);

                // 6. Save changes with concurrency handling
                try
                {
                    int affectedRows = await _context.SaveChangesAsync();

                    if (affectedRows == 0)
                    {
                        _logger.LogWarning("SaveChanges reported 0 affected rows");
                        return BadRequest("No changes detected in database.");
                    }

                    // 7. Verify update by fresh query
                    var updatedUser = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserId == userDto.OfficerId);

                    _logger.LogInformation("Updated user data: {@User}", updatedUser);

                   

                    return Ok(new
                    {
                        message = "User updated successfully.",
                        updatedData = new
                        {
                            updatedUser?.Fullname,
                            updatedUser?.Rank,
                            updatedUser?.Phone,
                            updatedUser?.Command
                        }
                    });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Concurrency conflict updating user {UserId}", userDto.OfficerId);
                    return Conflict("The record was modified by another user. Please refresh and try again.");
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating user");
                return BadRequest($"Database error: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating user");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPut("suspend-user/{userDelId}")]
        public async Task<IActionResult> SuspendUser(string userDelId)
        {
            try
            {
                // Find the user with the specified ID
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == Guid.Parse(userDelId));

                if (user == null)
                {
                    return BadRequest(new { message = "Error. User not found." });
                }

                // Update user status to 'Deleted'
                user.Status = "Suspended";
                _context.Users.Update(user);

                // Save changes to the database
                await _context.SaveChangesAsync();


                return Ok(new { message = "User successfully deleted." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error: {ex.Message}" });
            }
        }

        [HttpDelete("delete-user/{userDelId}")]
        public async Task<IActionResult> DeleteUser([FromRoute] Guid userDelId)
        {
            try
            {
                var user = _context.Users.Find(userDelId);

                Console.WriteLine(userDelId);
                if (user == null)
                {
                    return NotFound(new { message = "Error. User not found." });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();




                return Ok(new { message = "User successfully deleted." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }


        [HttpPut("unsuspend-user/{userDelId}")]
        public async Task<IActionResult> UnSuspendUser(string userDelId)
        {
            try
            {
                // Find the user with the specified ID
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == Guid.Parse(userDelId));

                if (user == null)
                {
                    return BadRequest(new { message = "Error. User not found." });
                }

                // Update user status to 'Deleted'
                user.Status = "Active";
                _context.Users.Update(user);

                // Save changes to the database
                await _context.SaveChangesAsync();


                return Ok(new { message = "User successfully deleted." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error: {ex.Message}" });
            }
        }

        [HttpPut("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] PasswordRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid data." });
            }

            try
            {
                // Retrieve the user ID from the JWT token claims (ensure user is logged in)
                var user = await _context.Users.FirstOrDefaultAsync(r => r.UserId == request.OfficerId);
                if (user == null)
                {
                    return Unauthorized(new { message = "User is not logged in." });
                }


                // Check if the old password matches
                if (user.Password != _generateToken.HashPassword(request.OldPassword))
                {
                    return BadRequest(new { message = "Incorrect old password." });
                }

                // Validate new password strength
                if (!ValidatePassword(request.NewPassword))
                {
                    return BadRequest(new
                    {
                        message = "Password should be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, one number, and one special character."
                    });
                }

                // Update password if valid
                user.Password = _generateToken.HashPassword(request.NewPassword);
                user.PassChange = user.PassChange + 1; // Assuming pass_change is a boolean
                _context.Users.Update(user);
                await _context.SaveChangesAsync();


                return Ok(new { message = "Password updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error: {ex.Message}" });
            }
        }


        [HttpPut("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid data." });
            }

            try
            {
                // Retrieve the user ID from the JWT token claims (ensure user is logged in)
                var user = await _context.Users.FirstOrDefaultAsync(r => r.UserId == request.OfficerId);
                if (user == null)
                {
                    return Unauthorized(new { message = "User is not found." });
                }




                // Validate new password strength
                if (!ValidatePassword(request.NewPassword))
                {
                    return BadRequest(new
                    {
                        message = "Password should be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, one number, and one special character."
                    });
                }

                // Update password if valid
                user.Password = _generateToken.HashPassword(request.NewPassword);
                user.PassChange = user.PassChange + 1; // Assuming pass_change is a boolean
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Password updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet("user")]
        public async Task<IActionResult> getUsers()
        {
            try
            {
                var user = await _context.Users

                    .Where(p => p.Status == "Active" || p.Status == "Suspended")
                    .OrderByDescending(r => r.DateCreated)
                    .Select(r => new
                    {
                        r.UserId,
                      
                        Email = r.Email,
                        CommandName = r.Command,
                           r.Zone,
                        Svn = r.Svn,
                      
                        AccessLevel = r.AccessLevel,
                        Fullname = r.Fullname,
                        Rank = r.Rank,
                        Status = r.Status,
                        Phone = r.Phone,
                        DateCreated = r.DateCreated,
                    })
                    .ToListAsync();

                return Ok(new { data = user });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

        public class ServerTableRequest
        {
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 20;
            public string? Search { get; set; }
            public string? Factory { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string? SortBy { get; set; }
            public string? SortOrder { get; set; } = "asc";
            public Guid? OfficerId { get; set; }
        }

        [HttpGet("users/server")]
        public async Task<IActionResult> GetServerUsers([FromQuery] ServerTableRequest request)
        {
            try
            {
                var query = _context.Users
                    .Where(p => p.Status == "Active" || p.Status == "Suspended");

                // ========== SERVER-SIDE FILTERING ==========

                // Search filter
                if (!string.IsNullOrEmpty(request.Search))
                {
                    query = query.Where(u =>
                        u.Email.Contains(request.Search) ||
                        u.Fullname.Contains(request.Search) ||
                        u.Svn.Contains(request.Search) ||
                         u.AccessLevel.Contains(request.Search) ||
                        u.Rank.Contains(request.Search));
                }

             


                // ========== DATE RANGE FILTER ==========

                // Start date filter
                if (request.StartDate.HasValue)
                {
                    var startDate = request.StartDate.Value.Date;
                    if (startDate.Kind == DateTimeKind.Unspecified)
                    {
                        startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
                    }
                    else
                    {
                        startDate = startDate.ToUniversalTime().Date;
                    }
                    query = query.Where(p => p.DateCreated >= startDate);
                }

                // End date filter
                if (request.EndDate.HasValue && request.StartDate.HasValue)
                {
                    var endDate = request.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                    if (endDate.Kind == DateTimeKind.Unspecified)
                    {
                        endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
                    }
                    else
                    {
                        endDate = endDate.ToUniversalTime();
                    }

                    var startDate = request.StartDate.Value.Date;
                    if (startDate.Kind == DateTimeKind.Unspecified)
                    {
                        startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
                    }
                    else
                    {
                        startDate = startDate.ToUniversalTime().Date;
                    }
                    query = query.Where(p => p.DateCreated >= startDate && p.DateCreated <= endDate);
                }

                // ========== SERVER-SIDE SORTING ==========
                query = request.SortBy?.ToLower() switch
                {
                    "fullname" => request.SortOrder == "desc"
                        ? query.OrderByDescending(u => u.Fullname)
                        : query.OrderBy(u => u.Fullname),
                    "email" => request.SortOrder == "desc"
                        ? query.OrderByDescending(u => u.Email)
                        : query.OrderBy(u => u.Email),
                    "datacreated" => request.SortOrder == "desc"
                        ? query.OrderByDescending(u => u.DateCreated)
                        : query.OrderBy(u => u.DateCreated),
                    "status" => request.SortOrder == "desc"
                        ? query.OrderByDescending(u => u.Status)
                        : query.OrderBy(u => u.Status),
                    _ => query.OrderByDescending(u => u.DateCreated)
                };

                // ========== SERVER-SIDE PAGINATION ==========
                var totalCount = await query.CountAsync();

                var users = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(r => new
                    {
                        r.UserId,
                        r.Command,
                        
                        Email = r.Email,
                        CommandName = r.Command,
                        Svn = r.Svn,
                       
                        AccessLevel = r.AccessLevel,
                        Fullname = r.Fullname,
                        Rank = r.Rank,
                        Status = r.Status,
                        Phone = r.Phone,
                        DateCreated = r.DateCreated,
                    })
                    .ToListAsync();

                return Ok(new
                {
                    data = users,
                    totalCount = totalCount,
                    page = request.Page,
                    pageSize = request.PageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

       
        private bool ValidatePassword(string password)
        {
            return password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => !char.IsLetterOrDigit(ch));
        }
        public class RefreshTokenRequest
        {
            public string RefreshToken { get; set; }
        }


      
    }
}
