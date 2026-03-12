using MediCore.API.Infrastructure.Database.Context;
using MediCore.API.Modules.Doctor.Models;
using MediCore.API.Modules.Patient.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MediCore.API.Modules.Doctor.Controllers
{
    [ApiController]
    [Route("api/doctors")]
    public class DoctorProfileController : ControllerBase
    {
        private readonly MediCoreDbContext _context;

        public DoctorProfileController(MediCoreDbContext context)
        {
            _context = context;
        }

        // GET api/doctors
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? departmentId = null)
        {
            var query = _context.DoctorProfiles
                .Include(d => d.User)
                .Include(d => d.Department)
                .Where(d => d.IsActive && d.User.IsActive)
                .AsQueryable();

            if (departmentId.HasValue)
                query = query.Where(d => d.DepartmentId == departmentId.Value);

            var doctors = await query.Select(d => new
            {
                d.Id,
                d.UserId,
                FullName = d.User.FullName,
                Email = d.User.Email,
                Phone = d.User.PhoneNumber,
                d.Specialization,
                d.Qualification,
                d.ExperienceYears,
                d.ConsultationFee,
                d.AvailableDays,
                d.MorningStart,
                d.MorningEnd,
                d.HasEveningShift,
                d.EveningStart,
                d.EveningEnd,
                d.SlotDurationMinutes,
                d.MaxPatientsPerDay,
                d.Bio,
                DepartmentId = d.DepartmentId,
                DepartmentName = d.Department.Name,
                DepartmentIcon = d.Department.Icon
            }).ToListAsync();

            return Ok(new { success = true, data = doctors });
        }
        // GET api/doctors/my
        [HttpGet("my")]
        [Authorize(Roles = "Doctor,Mentor")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr == null || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { success = false, message = "Invalid token" });

            var doctor = await _context.DoctorProfiles
                .Include(d => d.User)
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor profile not found" });

            return Ok(new { success = true, data = new { 
                doctor.Id, 
                doctor.UserId, 
                doctor.Specialization,
                doctor.DepartmentId,
                doctor.ConsultationFee,
                doctor.Qualification,
                FullName = doctor.User?.FullName,
                DepartmentName = doctor.Department?.Name
            } });
        }

        // GET api/doctors/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var doctor = await _context.DoctorProfiles
                .Include(d => d.User)
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor not found" });

            return Ok(new { success = true, data = doctor });
        }

        // POST api/doctors — Create doctor profile
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin")]
        public async Task<IActionResult> Create([FromBody] CreateDoctorProfileDto dto)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == dto.UserId);

                if (user == null)
                    return BadRequest(new { success = false, message = "User not found" });

                if (!user.UserRoles.Any(ur => ur.Role.Name == "Doctor" || ur.Role.Name == "Mentor"))
                    return BadRequest(new { success = false, message = "User must be a Doctor or Mentor" });

                var exists = await _context.DoctorProfiles
                    .AnyAsync(d => d.UserId == dto.UserId);

                if (exists)
                    return BadRequest(new { success = false, message = "Doctor profile already exists for this user" });

                var profile = new DoctorProfile
                {
                    UserId = dto.UserId,
                    DepartmentId = dto.DepartmentId,
                    Specialization = dto.Specialization ?? "",
                    Qualification = dto.Qualification ?? "",
                    ExperienceYears = dto.ExperienceYears,
                    ConsultationFee = dto.ConsultationFee,
                    AvailableDays = dto.AvailableDays ?? "Mon,Tue,Wed,Thu,Fri,Sat",
                    MorningStart = TimeSpan.Parse(dto.MorningStart ?? "09:00"),
                    MorningEnd = TimeSpan.Parse(dto.MorningEnd ?? "13:00"),
                    HasEveningShift = dto.HasEveningShift,
                    EveningStart = (dto.HasEveningShift && !string.IsNullOrEmpty(dto.EveningStart))
                        ? TimeSpan.Parse(dto.EveningStart) : (TimeSpan?)null,
                    EveningEnd = (dto.HasEveningShift && !string.IsNullOrEmpty(dto.EveningEnd))
                        ? TimeSpan.Parse(dto.EveningEnd) : (TimeSpan?)null,
                    SlotDurationMinutes = dto.SlotDurationMinutes,
                    MaxPatientsPerDay = dto.MaxPatientsPerDay,
                    Bio = dto.Bio ?? "",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.DoctorProfiles.Add(profile);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Doctor profile created successfully",
                    data = new
                    {
                        profile.Id,
                        profile.UserId,
                        FullName = user.FullName,
                        profile.Specialization,
                        profile.ConsultationFee,
                        profile.DepartmentId
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }
        // PUT api/doctors/5
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,Doctor")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateDoctorProfileDto dto)
        {
            var profile = await _context.DoctorProfiles.FindAsync(id);
            if (profile == null)
                return NotFound(new { success = false, message = "Profile not found" });

            profile.DepartmentId = dto.DepartmentId;
            profile.Specialization = dto.Specialization;
            profile.Qualification = dto.Qualification;
            profile.ExperienceYears = dto.ExperienceYears;
            profile.ConsultationFee = dto.ConsultationFee;
            profile.AvailableDays = dto.AvailableDays;
            profile.MorningStart = TimeSpan.Parse(dto.MorningStart);
            profile.MorningEnd = TimeSpan.Parse(dto.MorningEnd);
            profile.HasEveningShift = dto.HasEveningShift;
            profile.EveningStart = dto.HasEveningShift && !string.IsNullOrEmpty(dto.EveningStart) 
                ? TimeSpan.Parse(dto.EveningStart) : null;
            profile.EveningEnd = dto.HasEveningShift && !string.IsNullOrEmpty(dto.EveningEnd) 
                ? TimeSpan.Parse(dto.EveningEnd) : null;
            profile.SlotDurationMinutes = dto.SlotDurationMinutes;
            profile.MaxPatientsPerDay = dto.MaxPatientsPerDay;
            profile.Bio = dto.Bio;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = profile, message = "Profile updated" });
        }
    }

    public class CreateDoctorProfileDto
    {
        public int UserId { get; set; }
        public int DepartmentId { get; set; }
        public string Specialization { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public decimal ConsultationFee { get; set; }
        public string AvailableDays { get; set; } = "Mon,Tue,Wed,Thu,Fri,Sat";
        public string MorningStart { get; set; } = "09:00";
        public string MorningEnd { get; set; } = "13:00";
        public bool HasEveningShift { get; set; }
        public string? EveningStart { get; set; }
        public string? EveningEnd { get; set; }
        public int SlotDurationMinutes { get; set; } = 15;
        public int MaxPatientsPerDay { get; set; } = 30;
        public string Bio { get; set; } = string.Empty;
    }
}
