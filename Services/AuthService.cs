using System.Text;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Recipe_Sharing_Platform_API.JWT;
using Recipe_Sharing_Platform_API.DTO;
using System.IdentityModel.Tokens.Jwt;
using Recipe_Sharing_Platform_API.Data;
using Recipe_Sharing_Platform_API.Models;
using Recipe_Sharing_Platform_API.Interfaces;

namespace Recipe_Sharing_Platform_API.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly PasswordHasher<User> _hasher;
        private readonly JwtSettings _jwt;

        public AuthService(ApplicationDbContext db, IOptions<JwtSettings> jwtOptions)
        {
            _db = db;
            _hasher = new PasswordHasher<User>();
            _jwt = jwtOptions.Value;

            if (string.IsNullOrEmpty(_jwt.Key) || _jwt.Key.Length < 16) throw new ArgumentException("JWT Key must be at least 16 characters long");
        }

        private static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();

        private static bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 30) return false;
            var regex = new System.Text.RegularExpressions.Regex("^[a-z0-9._]+$");
            return regex.IsMatch(username);
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
        {
            var username = NormalizeUsername(req.Username);

            if (!IsValidUsername(username)) throw new ApplicationException("Username must be 3–30 chars, lowercase letters, numbers, dot or underscore only.");
            if (await _db.Users.AnyAsync(u => u.Username == username)) throw new ApplicationException("Username is already taken.");

            var user = new User
            {
                Username = username,
                Name = req.Name,
                PasswordHash = _hasher.HashPassword(null!, req.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return GenerateAuthResponse(user);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest req)
        {
            var username = NormalizeUsername(req.Username);
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username) ?? throw new ApplicationException("Invalid username or password.");
            var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);

            if (verify == PasswordVerificationResult.Failed) throw new ApplicationException("Invalid username or password.");
            return GenerateAuthResponse(user);
        }

        private AuthResponse GenerateAuthResponse(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwt.Key);

            if (key.Length < 16)
            {
                throw new ArgumentException("JWT key is too short. Must be at least 16 characters.");
            }

            var expiresAt = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("username", user.Username),
                new Claim("name", user.Name ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                Issuer = _jwt.Issuer,
                Audience = _jwt.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new AuthResponse
            {
                AccessToken = tokenString,
                ExpiresAt = expiresAt,
                Username = user.Username,
                Name = user.Name
            };
        }
    }
}