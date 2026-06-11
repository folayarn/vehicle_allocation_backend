using Vehicle_Information_System.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Vehicle_Information_System.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IConfiguration configuration, ILogger<TokenService> logger = null)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public (string AccessToken, string RefreshToken) GenerateNewToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Fullname ?? user.Email),
                new Claim(ClaimTypes.Role, user.AccessLevel ?? "User"),
                new Claim("UserId", user.UserId.ToString()),
                new Claim("UserType", "Fleet"),
                new Claim("AccessLevel", user.AccessLevel ?? "User"),
                new Claim("Fullname", user.Fullname ?? ""),
                new Claim("Email", user.Email)
            };

            return GenerateToken(claims, user, "Fleet");
        }

        public (string AccessToken, string RefreshToken) GenerateAccommodationToken(AccomodationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Fullname ?? user.Email),
                new Claim(ClaimTypes.Role, user.AccessLevel ?? "User"),
                new Claim("UserType", "Accommodation"),
                new Claim("AccessLevel", user.AccessLevel ?? "User"),
                new Claim("Fullname", user.Fullname ?? ""),
                new Claim("Email", user.Email),
                new Claim("Module", "Accommodation"),
            };

            return GenerateToken(claims, user, "Accommodation");
        }

        public (string AccessToken, string RefreshToken) GenerateStoreToken(StoreUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Fullname ?? user.Email),
                new Claim(ClaimTypes.Role, user.AccessLevel ?? "User"),
                new Claim("UserType", "Store"),
                new Claim("AccessLevel", user.AccessLevel ?? "User"),
                new Claim("Fullname", user.Fullname ?? ""),
                new Claim("Email", user.Email),
                new Claim("Module", "Store"),
            };

            return GenerateToken(claims, user, "Store");
        }

        public (string AccessToken, string RefreshToken) GenerateAssetToken(AssetUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Fullname ?? user.Email),
                new Claim(ClaimTypes.Role, user.AccessLevel ?? "User"),
                new Claim("UserType", "Asset"),
                new Claim("AccessLevel", user.AccessLevel ?? "User"),
                new Claim("Fullname", user.Fullname ?? ""),
                new Claim("Email", user.Email),
                new Claim("Module", "Asset"),
            };

            return GenerateToken(claims, user, "Asset");
        }

        private (string AccessToken, string RefreshToken) GenerateToken(List<Claim> claims, object user, string userType)
        {
            try
            {
                // Get configuration values with fallbacks
                var secretKey = _configuration["Jwt:Secret"] ??
                               _configuration["JWT:Secret"] ??
                               DotNetEnv.Env.GetString("Secret");

                var issuer = _configuration["Jwt:Issuer"] ??
                            _configuration["JWT:Issuer"] ??
                            "VehicleInformationSystem";

                var audience = _configuration["Jwt:Audience"] ??
                              _configuration["JWT:Audience"] ??
                              "VehicleInformationSystemUsers";

                var tokenExpiryMinutes = int.Parse(_configuration["Jwt:TokenExpiryMinutes"] ??
                                                   _configuration["JWT:TokenExpiryMinutes"] ??
                                                   "60");

                // Validate configuration
                if (string.IsNullOrEmpty(secretKey))
                    throw new Exception("JWT Secret key is missing from configuration");

                if (secretKey.Length < 32)
                {
                    _logger?.LogWarning("JWT Secret key is less than 32 characters. Consider using a stronger key.");
                }

                // Create security key
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                // Add standard claims
                var allClaims = new List<Claim>(claims)
                {
                    new Claim(JwtRegisteredClaimNames.Iat,
                        new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                        ClaimValueTypes.Integer64)
                };

                // Create token
                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: allClaims,
                    expires: DateTime.UtcNow.AddMinutes(tokenExpiryMinutes),
                    signingCredentials: credentials,
                    notBefore: DateTime.UtcNow
                );

                var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
                var refreshToken = GenerateRefreshToken();
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

                // Store refresh token in user object
                StoreRefreshToken(user, refreshToken, refreshTokenExpiry);

                // Log token generation (without sensitive data)
                _logger?.LogInformation("Token generated successfully for user type {UserType}, expires at {Expiry}",
                    userType, token.ValidTo);

                return (accessToken, refreshToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating token for user type {UserType}", userType);
                throw;
            }
        }

        private void StoreRefreshToken(object user, string refreshToken, DateTime refreshTokenExpiry)
        {
            switch (user)
            {
                case User fleetUser:
                    fleetUser.RefreshToken = refreshToken;
                    fleetUser.RefreshTokenExpiryTime = refreshTokenExpiry;
                    break;

                case AccomodationUser accommodationUser:
                    accommodationUser.RefreshToken = refreshToken;
                    accommodationUser.RefreshTokenExpiryTime = refreshTokenExpiry;
                    break;

                case StoreUser storeUser:
                    storeUser.RefreshToken = refreshToken;
                    storeUser.RefreshTokenExpiryTime = refreshTokenExpiry;
                    break;

                case AssetUser assetUser:
                    assetUser.RefreshToken = refreshToken;
                    assetUser.RefreshTokenExpiryTime = refreshTokenExpiry;
                    break;

                default:
                    _logger?.LogWarning("Unknown user type when storing refresh token: {UserType}", user.GetType().Name);
                    break;
            }
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64]; // Increased to 64 bytes for better security
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty");

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public bool VerifyPassword(string inputPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedHash))
                return false;

            var inputHash = HashPassword(inputPassword);
            return string.Equals(inputHash, storedHash, StringComparison.Ordinal);
        }

       

        public bool ValidateToken(string token)
        {
            try
            {
                var secretKey = _configuration["Jwt:Secret"] ??
                               _configuration["JWT:Secret"] ??
                               DotNetEnv.Env.GetString("Secret");

                var issuer = _configuration["Jwt:Issuer"] ??
                            _configuration["JWT:Issuer"] ??
                            "VehicleInformationSystem";

                var audience = _configuration["Jwt:Audience"] ??
                              _configuration["JWT:Audience"] ??
                              "VehicleInformationSystemUsers";

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateLifetime = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}