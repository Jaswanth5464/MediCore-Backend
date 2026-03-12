using MediCore.API.Infrastructure.Database.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCore.API.Modules.Patient.Controllers
{
    [ApiController]
    [Route("api/departments")]
    public class DepartmentController : ControllerBase
    {
        private readonly MediCoreDbContext _context;

        public DepartmentController(MediCoreDbContext context)
        {
            _context = context;
        }

        // GET api/departments — Public, no auth needed
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var departments = await _context.Departments
                .Where(d => d.IsActive)
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.Description,
                    d.Icon,
                    d.FloorNumber,
                    d.IsActive,
                    DoctorCount = d.DoctorProfiles.Count(dp => dp.IsActive)
                })
                .OrderBy(d => d.Name)
                .ToListAsync();

            return Ok(new { success = true, data = departments });
        }

        // POST api/departments — Admin only
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin")]
        public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto)
        {
            var dept = new Models.Department
            {
                Name = dto.Name,
                Description = dto.Description,
                Icon = dto.Icon,
                FloorNumber = dto.FloorNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = dept, message = "Department created" });
        }

        // PUT api/departments/1
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateDepartmentDto dto)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound(new { success = false, message = "Department not found" });

            dept.Name = dto.Name;
            dept.Description = dto.Description;
            dept.Icon = dto.Icon;
            dept.FloorNumber = dto.FloorNumber;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = dept, message = "Department updated" });
        }

        // PATCH api/departments/1/toggle
        [HttpPatch("{id}/toggle")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin")]
        public async Task<IActionResult> Toggle(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound(new { success = false, message = "Not found" });

            dept.IsActive = !dept.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Department {(dept.IsActive ? "activated" : "deactivated")}" });
        }
    }

    public class CreateDepartmentDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
    }
}