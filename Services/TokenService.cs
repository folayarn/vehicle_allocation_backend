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

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (string AccessToken, string RefreshToken) GenerateNewToken(User user)
        {
            // Get values from configuration
            var secretKey = _configuration["Jwt:Secret"] ?? DotNetEnv.Env.GetString("Secret");
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            // Debug output to verify values
            Console.WriteLine("=== TOKEN GENERATION ===");
            Console.WriteLine($"Secret Key: {(string.IsNullOrEmpty(secretKey) ? "MISSING" : "LOADED")}");
            Console.WriteLine($"Issuer: '{issuer}'");
            Console.WriteLine($"Audience: '{audience}'");
            Console.WriteLine($"User: {user.Email}");

            // Validate values
            if (string.IsNullOrEmpty(issuer))
                throw new Exception("Issuer is null or empty!");
            if (string.IsNullOrEmpty(audience))
                throw new Exception("Audience is null or empty!");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("UserId", user.UserId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.AccessLevel ?? "user")
            };

            // Create token with explicit issuer and audience
            var token = new JwtSecurityToken(
                issuer: issuer,      // MUST be set
                audience: audience,  // MUST be set
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = GenerateRefreshToken();

            // Verify the token has issuer and audience
            var handler = new JwtSecurityTokenHandler();
            var readToken = handler.ReadJwtToken(accessToken);
            Console.WriteLine($"Generated Token Issuer: '{readToken.Issuer}'");
            Console.WriteLine($"Generated Token Audience: '{string.Join(",", readToken.Audiences ?? new string[0])}'");
            Console.WriteLine($"Generated Token Expiry: {readToken.ValidTo}");

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            return (AccessToken: accessToken, RefreshToken: refreshToken);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}