using MediCore.API.Infrastructure.Database.Context;
using MediCore.API.Modules.Doctor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MediCore.API.Modules.Doctor.Controllers
{
    [ApiController]
    [Route("api/doctor-leaves")]
    public class DoctorLeaveController : ControllerBase
    {
        private readonly MediCoreDbContext _context;

        public DoctorLeaveController(MediCoreDbContext context)
        {
            _context = context;
        }

        [HttpGet("my")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetMyLeaves()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr == null || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { success = false, message = "Invalid token" });

            var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor profile not found" });

            var leaves = await _context.DoctorLeaves
                .Where(l => l.DoctorProfileId == doctor.Id)
                .OrderByDescending(l => l.StartDate)
                .ToListAsync();

            return Ok(new { success = true, data = leaves });
        }

        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CreateLeave([FromBody] CreateLeaveDto dto)
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr == null || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { success = false, message = "Invalid token" });

            var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor profile not found" });

            var leave = new DoctorLeave
            {
                DoctorProfileId = doctor.Id,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Reason = dto.Reason,
                Status = "Pending", // Now requires admin approval
                CreatedAt = DateTime.UtcNow
            };

            _context.DoctorLeaves.Add(leave);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Leave request submitted successfully", data = leave });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CancelLeave(int id)
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr == null || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { success = false, message = "Invalid token" });

            var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor profile not found" });

            var leave = await _context.DoctorLeaves.FindAsync(id);
            if (leave == null)
                return NotFound(new { success = false, message = "Leave record not found" });

            if (leave.DoctorProfileId != doctor.Id)
                return Forbid();

            _context.DoctorLeaves.Remove(leave);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Leave cancelled successfully" });
        }

        // --- ADMIN ENDPOINTS ---

        [HttpGet("all")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin")]
        public async Task<IActionResult> GetAllLeaves()
        {
            var leaves = await _context.DoctorLeaves
                .Include(l => l.DoctorProfile)
                .ThenInclude(dp => dp.User)
                .Include(l => l.DoctorProfile)
                .ThenInclude(dp => dp.Department)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new
                {
                    l.Id,
                    DoctorName = l.DoctorProfile.User.FullName,
                    DepartmentName = l.DoctorProfile.Department.Name,
                    l.StartDate,
                    l.EndDate,
                    l.Reason,
                    l.Status,
                    l.CreatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = leaves });
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin")]
        public async Task<IActionResult> UpdateLeaveStatus(int id, [FromBody] UpdateLeaveStatusDto dto)
        {
            if (dto.Status != "Approved" && dto.Status != "Rejected" && dto.Status != "Pending")
                return BadRequest(new { success = false, message = "Invalid status" });

            var leave = await _context.DoctorLeaves.FindAsync(id);
            if (leave == null)
                return NotFound(new { success = false, message = "Leave record not found" });

            leave.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Leave {dto.Status.ToLower()} successfully", data = leave });
        }
    }

    public class UpdateLeaveStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class CreateLeaveDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
