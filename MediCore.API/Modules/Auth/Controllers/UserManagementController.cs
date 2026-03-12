using MediCore.API.Common.Enums;
using MediCore.API.Contracts.Requests;
using MediCore.API.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCore.API.Modules.Auth.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "SuperAdmin,HospitalAdmin")]
    public class UserManagementController : ControllerBase
    {
        private readonly IUserManagementService _userService;

        public UserManagementController(IUserManagementService userService)
        {
            _userService = userService;
        }

        // GET api/users
        // GET api/users?roleId=4
        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] int? roleId = null)
        {
            var result = await _userService.GetAllUsersAsync(roleId);
            return Ok(result);
        }



        // GET api/users/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var result = await _userService.GetUserByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        // POST api/users
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var result = await _userService.CreateUserAsync(request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // PUT api/users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(
            int id, [FromBody] UpdateUserRequest request)
        {
            var result = await _userService.UpdateUserAsync(id, request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // PATCH api/users/5/toggle-status
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var result = await _userService.ToggleUserStatusAsync(id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // PATCH api/users/5/reset-password
        [HttpPatch("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(
            int id, [FromBody] ResetPasswordRequest request)
        {
            var result = await _userService.ResetPasswordAsync(id, request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}

// Separate controller so Doctor/Receptionist [Authorize] is not blocked
// by the UserManagementController class-level SuperAdmin-only restriction.
[ApiController]
[Route("api/users")]
public class UserSearchController : ControllerBase
{
    private readonly IUserManagementService _userService;

    public UserSearchController(IUserManagementService userService)
    {
        _userService = userService;
    }

    // GET api/users/search?q=john
    [HttpGet("search")]
    [Authorize(Roles = "SuperAdmin,HospitalAdmin,Doctor,Receptionist")]
    public async Task<IActionResult> SearchUsers([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new { success = true, data = new List<object>() });

        var result = await _userService.SearchPatientsAsync(q);
        return Ok(result);
    }
}