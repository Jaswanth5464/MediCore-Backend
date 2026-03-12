using MediCore.API.Contracts.Requests;
using MediCore.API.Contracts.Responses;
using MediCore.API.Infrastructure.Database.Context;
using MediCore.API.Modules.Auth.Models;
using Microsoft.EntityFrameworkCore;

namespace MediCore.API.Modules.Auth.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
        Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse<AuthResponse>> RefreshTokenAsync(string refreshToken);
        Task<ApiResponse<string>> LogoutAsync(string refreshToken);
        Task<ApiResponse<string>> RevokeAllTokensAsync(int userId);
    }

    public class AuthService : IAuthService
    {
        private readonly MediCoreDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;

        public AuthService(
            MediCoreDbContext context,
            ITokenService tokenService,
            IConfiguration configuration)
        {
            _context = context;
            _tokenService = tokenService;
            _configuration = configuration;
        }

        public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            // Check if email already exists
            var existingUser = await _context.Users
                .AnyAsync(u => u.Email == request.Email);

            if (existingUser)
                return ApiResponse<AuthResponse>.Fail("Email already registered.");

            // Check if role exists
            var role = await _context.Roles.FindAsync(request.RoleId);
            if (role == null)
                return ApiResponse<AuthResponse>.Fail("Invalid role selected.");

            // Hash the password using BCrypt
            // Never store plain text passwords
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = passwordHash,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UserRoles = new List<UserRole> { new UserRole { RoleId = request.RoleId } }
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Load role for token generation
            user.UserRoles.First().Role = role;

            return await GenerateAuthResponse(user);
        }

        public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            // Find user by email and include Role data
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                return ApiResponse<AuthResponse>.Fail("Invalid email or password.");

            if (!user.IsActive)
                return ApiResponse<AuthResponse>.Fail("Account is deactivated.");

            // Verify password against stored hash
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
                return ApiResponse<AuthResponse>.Fail("Invalid email or password.");

            return await GenerateAuthResponse(user);
        }
        private DateTime GetIndianTime()
        {
            var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indiaTimeZone);
        }
        public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(string refreshToken)
        {
            // Find the refresh token in database
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null)
                return ApiResponse<AuthResponse>.Fail("Invalid refresh token.");

            if (storedToken.IsRevoked)
                return ApiResponse<AuthResponse>.Fail("Refresh token has been revoked.");

            if (storedToken.ExpiresAt < DateTime.UtcNow)
                return ApiResponse<AuthResponse>.Fail("Refresh token has expired.");

            // Revoke old token - rotation security
            storedToken.IsRevoked = true;
            await _context.SaveChangesAsync();

            return await GenerateAuthResponse(storedToken.User);
        }

        private async Task<ApiResponse<AuthResponse>> GenerateAuthResponse(User user)
        {
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var refreshTokenExpiry = int.Parse(
                _configuration["JwtSettings:RefreshTokenExpiryDays"]!);

            // Save new refresh token to database
            var indianTime = GetIndianTime();

            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = indianTime.AddDays(refreshTokenExpiry),
                IsRevoked = false,
                CreatedAt = indianTime
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                FullName = user.FullName,
                Email = user.Email,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };

            return ApiResponse<AuthResponse>.Ok(response, "Authentication successful.");
        }
        public async Task<ApiResponse<string>> LogoutAsync(string refreshToken)
        {
            // Find the refresh token in database
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null)
                return ApiResponse<string>.Fail("Invalid token.");

            // Revoke it immediately
            storedToken.IsRevoked = true;
            await _context.SaveChangesAsync();

            return ApiResponse<string>.Ok("Logged out successfully.");
        }

        public async Task<ApiResponse<string>> RevokeAllTokensAsync(int userId)
        {
            // Find ALL active tokens for this user
            var allTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            // Revoke every single one
            foreach (var token in allTokens)
            {
                token.IsRevoked = true;
            }

            await _context.SaveChangesAsync();

            return ApiResponse<string>.Ok(
                $"All tokens revoked. {allTokens.Count} sessions terminated.");
        }
    }
}