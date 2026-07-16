using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Servidor.KubeOrchestratorPlatform.Database.Data;
using Servidor.KubeOrchestratorPlatform.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Servidor.KubeOrchestratorPlatform.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext dbContext;
        private readonly IConfiguration configuration;

        public AuthService(AppDbContext dbContext, IConfiguration configuration)
        {
            this.dbContext = dbContext;
            this.configuration = configuration;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto request)
        {
            if (await dbContext.Users.AnyAsync(u => u.Email == request.Email))
                throw new Exception("El email ya está registrado.");

            if (await dbContext.Users.AnyAsync(u => u.Username == request.Username))
                throw new Exception("El nombre de usuario ya está en uso.");

            bool isFirstUser = !await dbContext.Users.AnyAsync();

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = isFirstUser ? "Admin" : "User",
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            return await CreateTokenResponseAsync(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto request)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new Exception("Credenciales inválidas.");
            }

            user.LastLoginAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            return await CreateTokenResponseAsync(user);
        }

        public async Task<AuthResponseDto> RefreshAsync(string refreshToken)
        {
            var tokenHash = HashToken(refreshToken);
            var stored = await dbContext.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (stored == null || stored.RevokedAt != null || stored.ExpiresAt <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("El refresh token es inválido o expiró.");
            }

            stored.RevokedAt = DateTime.UtcNow;

            return await CreateTokenResponseAsync(stored.User);
        }

        public async Task RevokeAsync(string refreshToken)
        {
            var tokenHash = HashToken(refreshToken);
            var stored = await dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (stored == null || stored.RevokedAt != null)
            {
                return;
            }

            stored.RevokedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        private async Task<AuthResponseDto> CreateTokenResponseAsync(User user)
        {
            var accessToken = GenerateJwtToken(user);
            var refreshTokenValue = GenerateRefreshTokenValue();

            dbContext.RefreshTokens.Add(new RefreshToken
            {
                TokenHash = HashToken(refreshTokenValue),
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(configuration["Jwt:RefreshTokenExpirationDays"]))
            });

            await dbContext.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = accessToken,
                RefreshToken = refreshTokenValue,
                Username = user.Username,
                Email = user.Email,
                IsAdmin = user.Role == "Admin"
            };
        }

        private static string GenerateRefreshTokenValue()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        private static string HashToken(string token)
        {
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }

        private string GenerateJwtToken(User user)
        {
            var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);

            var claims = new List<Claim>
            {
                new Claim("id", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddMinutes(Convert.ToDouble(configuration["Jwt:AccessTokenExpirationMinutes"]));

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
