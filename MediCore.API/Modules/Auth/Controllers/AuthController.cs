using MediCore.API.Contracts.Requests;
using MediCore.API.Infrastructure.Database.Context;
using MediCore.API.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MediCore.API.Modules.Auth.Controllers
{
	[ApiController]
	[Route("api/auth")]
	public class AuthController : ControllerBase
	{
		private readonly IAuthService _authService;
        private readonly MediCoreDbContext _context;

        public AuthController(IAuthService authService, MediCoreDbContext context)
		{
			_authService = authService;
            _context = context;
        }

		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegisterRequest request)
		{
			var result = await _authService.RegisterAsync(request);
			if (!result.Success)
				return BadRequest(result);
			return Ok(result);
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequest request)
		{
			var result = await _authService.LoginAsync(request);
			if (!result.Success)
				return BadRequest(result);
			return Ok(result);
		}

		[HttpPost("refresh")]
		public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
		{
			var result = await _authService.RefreshTokenAsync(request.RefreshToken);
			if (!result.Success)
				return BadRequest(result);
			return Ok(result);
		}


        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.LogoutAsync(request.RefreshToken);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("revoke-all")]
        [Authorize]
        public async Task<IActionResult> RevokeAll()
        {
            // Get userId from JWT token claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            var userId = int.Parse(userIdClaim);
            var result = await _authService.RevokeAllTokensAsync(userId);
            return Ok(result);
        }

        [HttpGet("roles")]
        [Authorize]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _context.Roles
                .Select(r => new { r.Id, r.Name, r.Description })
                .ToListAsync();

            return Ok(new { success = true, data = roles });
        }
    }
}