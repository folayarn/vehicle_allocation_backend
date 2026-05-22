using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Vehicle_Information_System.Dtos;
using Vehicle_Information_System.Models;
using Vehicle_Information_System.Services;
using static Vehicle_Information_System.Controllers.UserController;


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
     
       

        public AuthController(ApplicationDbContext context,
           IConfiguration configuration,
            UserService userService,
            TokenService generateToken)
        {
          
        _userService = userService;
           
            _configuration = configuration;
            _context = context;
            _generateToken = generateToken;
           
        }

        [HttpGet]
        public IActionResult Index()
        {
            var message = new { Text = "Welcome home!! API is working" }; // Returning an anonymous object as JSON
            return Ok(message);  // 200 OK response with JSON content
        }


        [HttpPost("debug-token-full")]
        [AllowAnonymous]
        public async Task<IActionResult> DebugTokenFull([FromHeader(Name = "Authorization")] string authorization)
        {
            try
            {
                Console.WriteLine("=== FULL TOKEN DEBUG ===");

                // Check if authorization header exists
                if (string.IsNullOrEmpty(authorization))
                {
                    return BadRequest(new { error = "No Authorization header" });
                }

                Console.WriteLine($"Authorization header: {authorization}");

                // Extract token
                var token = authorization.Replace("Bearer ", "").Trim();
                Console.WriteLine($"Token extracted: {token.Substring(0, Math.Min(50, token.Length))}...");

                // 1. Decode token without validation
                var handler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwtToken;
                try
                {
                    jwtToken = handler.ReadJwtToken(token);
                    Console.WriteLine("Token decoded successfully");
                    Console.WriteLine($"Token Issuer: {jwtToken.Issuer}");
                    Console.WriteLine($"Token Audience: {string.Join(",", jwtToken.Audiences ?? new List<string>())}");
                    Console.WriteLine($"Token Expiry: {jwtToken.ValidTo}");
                    Console.WriteLine($"Token Claims: {string.Join(", ", jwtToken.Claims.Select(c => $"{c.Type}={c.Value}"))}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to decode token: {ex.Message}");
                    return BadRequest(new { error = $"Cannot decode token: {ex.Message}" });
                }

                Console.WriteLine("Decoded token successfully. Now validating...");
                Console.WriteLine("=== VALIDATION PARAMETERS ===");
                Console.WriteLine($"Token validation will be performed with the following parameters:");
                Console.WriteLine(jwtToken);


                // 2. Get validation parameters from your configuration
                var secretKey = _configuration["Jwt:Secret"] ?? DotNetEnv.Env.GetString("Secret");
                var issuer = _configuration["Jwt:Issuer"];
                var audience = _configuration["Jwt:Audience"];

                Console.WriteLine($"Validation parameters:");
                Console.WriteLine($"  Expected Issuer: {issuer}");
                Console.WriteLine($"  Expected Audience: {audience}");
                Console.WriteLine($"  Secret loaded: {(string.IsNullOrEmpty(secretKey) ? "NO" : "YES")}");
                Console.WriteLine($"  Secret length: {secretKey?.Length ?? 0}");

                var key = Encoding.UTF8.GetBytes(secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                // 3. Try to validate the token
                try
                {
                    var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);
                    Console.WriteLine("✅ TOKEN VALIDATED SUCCESSFULLY!");

                    return Ok(new
                    {
                        valid = true,
                        message = "Token is valid",
                        claims = principal.Claims.Select(c => new { c.Type, c.Value }),
                        expiry = jwtToken.ValidTo
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Validation failed: {ex.Message}");
                    Console.WriteLine($"Exception type: {ex.GetType().Name}");

                    return BadRequest(new
                    {
                        valid = false,
                        error = ex.Message,
                        error_type = ex.GetType().Name,
                        token_info = new
                        {
                            issuer_from_token = jwtToken.Issuer,
                            audience_from_token = jwtToken.Audiences?.FirstOrDefault(),
                            expected_issuer = issuer,
                            expected_audience = audience,
                            token_expiry = jwtToken.ValidTo,
                            current_time = DateTime.UtcNow,
                            is_expired = jwtToken.ValidTo < DateTime.UtcNow
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpPost("debug-validate-token")]
        [AllowAnonymous]
        public IActionResult DebugValidateToken([FromBody] string token)
        {
            try
            {
                Console.WriteLine("=== DEBUG VALIDATE TOKEN ===");
                Console.WriteLine($"Token to validate: {token?.Substring(0, Math.Min(50, token?.Length ?? 0))}...");

                var tokenHandler = new JwtSecurityTokenHandler();
                var secretKey = DotNetEnv.Env.GetString("Secret") ?? _configuration["Jwt:Secret"];

                Console.WriteLine($"Secret key used: {(string.IsNullOrEmpty(secretKey) ? "NULL" : "Loaded")}");
                Console.WriteLine($"Secret key length: {secretKey?.Length ?? 0}");

                var key = Encoding.UTF8.GetBytes(secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                try
                {
                    var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                    var jwtToken = validatedToken as JwtSecurityToken;

                    return Ok(new
                    {
                        valid = true,
                        claims = principal.Claims.Select(c => new { c.Type, c.Value }),
                        expires = jwtToken?.ValidTo,
                        issuer_match = jwtToken?.Issuer == _configuration["Jwt:Issuer"],
                        audience_match = jwtToken?.Audiences?.Contains(_configuration["Jwt:Audience"]) ?? false
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Validation failed: {ex.Message}");
                    return BadRequest(new
                    {
                        valid = false,
                        error = ex.Message,
                        error_type = ex.GetType().Name
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpGet("debug-token-info")]
        [AllowAnonymous]
        public IActionResult DebugTokenInfo()
        {
            try
            {
                var secretFromEnv = DotNetEnv.Env.GetString("Secret");
                var secretFromConfig = _configuration["Jwt:Secret"];
                var issuer = _configuration["Jwt:Issuer"];
                var audience = _configuration["Jwt:Audience"];

                return Ok(new
                {
                    secret_from_env_exists = !string.IsNullOrEmpty(secretFromEnv),
                    secret_from_config_exists = !string.IsNullOrEmpty(secretFromConfig),
                    secret_length_env = secretFromEnv?.Length ?? 0,
                    secret_length_config = secretFromConfig?.Length ?? 0,
                    issuer = issuer,
                    audience = audience,
                    secrets_match = secretFromEnv == secretFromConfig,
                    message = "Use this info to debug token issues"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        [HttpPost("generate-otp")]
        public async Task<IActionResult> Login(AuthenticationDto userAuth)
        {
            if (userAuth == null || string.IsNullOrEmpty(userAuth.Email) || string.IsNullOrEmpty(userAuth.Password))
            {
                return BadRequest(new { message = "Invalid email or password." });
            }

            try
            {
                // Fetch the user by email
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == userAuth.Email);

                if (user == null)
                {
                    // Avoid specifying whether the issue is with email or password
                    return BadRequest(new { message = "Invalid credentials." });
                }

                // Verify the password using your password hashing mechanism
                if (!VerifyPassword(userAuth.Password, user.Password))
                {
                    return BadRequest(new { message = "Invalid credentials." });
                }

                // Generate OTP
             
           


                    return Ok(new
                    {
                        message = "OTP sent to your email"


                    }
                    );




            }
            catch (Exception ex)
            {
                // Log the exception (in real scenarios, use a logging framework like Serilog or NLog)
                Console.WriteLine($"Login error: {ex.Message}");

                // Avoid returning stack traces in production
                return BadRequest(new { message = "An error occurred while processing your request." });
            }
        }


    
        public class OtpVerificationDto
        {
            public string Email { get; set; }
            public string Password { get; set; }


        }

        [HttpGet("test-generate")]
        [AllowAnonymous]
        public IActionResult TestGenerate()
        {
            try
            {
                // Create a test user
                var testUser = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = "test@example.com",
                    AccessLevel = "admin"
                };

                // Generate token
                var (accessToken, refreshToken) = _generateToken.GenerateNewToken(testUser);

                // Decode and verify
                var handler = new JwtSecurityTokenHandler();
                var decoded = handler.ReadJwtToken(accessToken);

                return Ok(new
                {
                    token = accessToken,
                    decoded_issuer = decoded.Issuer,
                    decoded_audience = decoded.Audiences?.FirstOrDefault(),
                    decoded_claims = decoded.Claims.Select(c => new { c.Type, c.Value }),
                    has_issuer = !string.IsNullOrEmpty(decoded.Issuer),
                    has_audience = decoded.Audiences?.Any() == true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpGet("check-config")]
        [AllowAnonymous]
        public IActionResult CheckConfig()
        {
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var secret = _configuration["Jwt:Secret"];

            return Ok(new
            {
                issuer_from_config = issuer ?? "NULL",
                audience_from_config = audience ?? "NULL",
                secret_loaded = !string.IsNullOrEmpty(secret)
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] OtpVerificationDto otpVerification)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == otpVerification.Email);

                if (user == null)
                    return BadRequest(new { message = "Invalid credentials" });

                if (user.Status != "Active")
                    return BadRequest(new { message = "Account is inactive" });

                if (!VerifyPassword(otpVerification.Password, user.Password))
                    return BadRequest(new { message = "Invalid credentials" });

                // Generate tokens
                var (accessToken, refreshToken) = _generateToken.GenerateNewToken(user);

                // Save refresh token to database
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Login successful",
                    user_token = accessToken,
                    refresh_token = refreshToken,
                    user_access_level = user.AccessLevel,
                    id = user.UserId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var user = await _userService.GetUserByRefreshToken(request.RefreshToken);
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Unauthorized("Invalid or expired refresh token");
            }

            var tokens = _generateToken.GenerateNewToken(user);

            // Update user's refresh token
            user.RefreshToken = tokens.RefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userService.UpdateUserAsync(user);

            return Ok(new
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken
            });
        }


        private bool VerifyPassword(string inputPassword, string storedHashedPassword)
        {
            // Replace with actual hashing comparison (e.g., BCrypt or Argon2)
            var hashInputPassword = _generateToken.HashPassword(inputPassword);
            return hashInputPassword == storedHashedPassword;
            
        }
       
    }
}
