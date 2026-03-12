using MediCore.API.Contracts.Requests;
using MediCore.API.Contracts.Responses;
using MediCore.API.Infrastructure.Database.Context;
using MediCore.API.Modules.Auth.Models;
using Microsoft.EntityFrameworkCore;

namespace MediCore.API.Modules.Auth.Services
{
    public interface IUserManagementService
    {
        Task<ApiResponse<UserListResponse>> GetAllUsersAsync(int? roleId = null);
        Task<ApiResponse<UserResponse>> GetUserByIdAsync(int id);
        Task<ApiResponse<UserResponse>> CreateUserAsync(CreateUserRequest request);
        Task<ApiResponse<UserResponse>> UpdateUserAsync(int id, UpdateUserRequest request);
        Task<ApiResponse<string>> ToggleUserStatusAsync(int id);
        Task<ApiResponse<string>> ResetPasswordAsync(int id, ResetPasswordRequest request);
        Task<ApiResponse<List<PatientSearchResult>>> SearchPatientsAsync(string query); // ← ADD THIS
    }

    public class UserManagementService : IUserManagementService
    {
        private readonly MediCoreDbContext _context;

        public UserManagementService(MediCoreDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<UserListResponse>> GetAllUsersAsync(int? roleId = null)
        {
            var query = _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsQueryable();

            if (roleId.HasValue)
                query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId.Value));

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var response = new UserListResponse
            {
                Users = users.Select(MapToResponse).ToList(),
                TotalCount = users.Count,
                ActiveCount = users.Count(u => u.IsActive),
                InactiveCount = users.Count(u => !u.IsActive)
            };

            return ApiResponse<UserListResponse>.Ok(response);
        }

        public async Task<ApiResponse<UserResponse>> GetUserByIdAsync(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return ApiResponse<UserResponse>.Fail("User not found.");

            return ApiResponse<UserResponse>.Ok(MapToResponse(user));
        }

        public async Task<ApiResponse<UserResponse>> CreateUserAsync(CreateUserRequest request)
        {
            var exists = await _context.Users
                .AnyAsync(u => u.Email == request.Email);

            if (exists)
                return ApiResponse<UserResponse>.Fail("Email already registered.");

            var role = await _context.Roles.FindAsync(request.RoleId);
            if (role == null)
                return ApiResponse<UserResponse>.Fail("Invalid role.");

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UserRoles = new List<UserRole> { new UserRole { RoleId = request.RoleId } }
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            user.UserRoles.First().Role = role;
            return ApiResponse<UserResponse>.Ok(
                MapToResponse(user), "User created successfully.");
        }

        public async Task<ApiResponse<UserResponse>> UpdateUserAsync(
            int id, UpdateUserRequest request)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return ApiResponse<UserResponse>.Fail("User not found.");

            user.FullName = request.FullName;
            user.PhoneNumber = request.PhoneNumber;
            user.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return ApiResponse<UserResponse>.Ok(
                MapToResponse(user), "User updated successfully.");
        }

        public async Task<ApiResponse<string>> ToggleUserStatusAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return ApiResponse<string>.Fail("User not found.");

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            var status = user.IsActive ? "activated" : "deactivated";
            return ApiResponse<string>.Ok($"User {status} successfully.");
        }

        public async Task<ApiResponse<string>> ResetPasswordAsync(
            int id, ResetPasswordRequest request)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return ApiResponse<string>.Fail("User not found.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return ApiResponse<string>.Ok("Password reset successfully.");
        }

        // ← ADD THIS METHOD
        public async Task<ApiResponse<List<PatientSearchResult>>> SearchPatientsAsync(string query)
        {
            var lowerQuery = query.ToLower();

            var patients = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u =>
                    u.IsActive &&
                    u.UserRoles.Any(ur => ur.Role.Name == "Patient") &&
                    (u.FullName.ToLower().Contains(lowerQuery) ||
                     u.PhoneNumber.Contains(query) ||
                     u.Email.ToLower().Contains(lowerQuery)))
                .OrderBy(u => u.FullName)
                .Take(10)
                .Select(u => new PatientSearchResult
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    PhoneNumber = u.PhoneNumber,
                    Email = u.Email
                })
                .ToListAsync();

            return ApiResponse<List<PatientSearchResult>>.Ok(patients);
        }

        private static UserResponse MapToResponse(User user) => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>(),
            RoleIds = user.UserRoles?.Select(ur => ur.RoleId).ToList() ?? new List<int>(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    // ← ADD THIS CLASS (can be in Contracts/Responses folder if you prefer)
    public class PatientSearchResult
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}