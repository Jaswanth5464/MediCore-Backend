using MediCore.API.Infrastructure.Database.Context;
using MediCore.API.Modules.Laboratory.Models;
using MediCore.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCore.API.Modules.Laboratory.Controllers
{
    [ApiController]
    [Route("api/laboratory")]
    [Authorize(Roles = "SuperAdmin,HospitalAdmin,LabTechnician")]
    public class LaboratoryController : ControllerBase
    {
        private readonly MediCoreDbContext _context;
        private readonly IEmailService _emailService;

        public LaboratoryController(MediCoreDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET api/laboratory/orders
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] string? status = null)
        {
            var query = _context.LabOrders.AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
                query = query.Where(l => l.Status == status);

            var result = await query.OrderByDescending(l => l.CreatedAt).Select(l => new
            {
                l.Id,
                l.AppointmentId,
                l.TestType,
                l.Notes,
                l.Status,
                l.ResultNotes,
                l.ReportUrl,
                l.CreatedAt,
                l.CompletedAt,
                PatientName = _context.Users.Where(u => u.Id == l.PatientUserId).Select(u => u.FullName).FirstOrDefault(),
                DoctorName = _context.DoctorProfiles.Where(d => d.Id == l.DoctorProfileId)
                    .Select(d => _context.Users.Where(u => u.Id == d.UserId).Select(u => u.FullName).FirstOrDefault())
                    .FirstOrDefault()
            }).ToListAsync();

            return Ok(new { success = true, data = result });
        }

        // PATCH api/laboratory/orders/{id}/complete
        [HttpPatch("orders/{id}/complete")]
        public async Task<IActionResult> CompleteOrder(int id, [FromBody] CompleteOrderDto dto)
        {
            var order = await _context.LabOrders.FindAsync(id);
            if (order == null) return NotFound(new { success = false, message = "Order not found" });

            order.Status = "Completed";
            order.ResultNotes = dto.ResultNotes;
            order.ReportUrl = dto.ReportUrl;
            order.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Lab results updated successfully" });
        }

        // POST api/laboratory/orders/{id}/email
        [HttpPost("orders/{id}/email")]
        public async Task<IActionResult> EmailReport(int id)
        {
            var order = await _context.LabOrders.FindAsync(id);
            if (order == null) return NotFound(new { success = false, message = "Order not found" });
            if (order.Status != "Completed") return BadRequest(new { success = false, message = "Order is not yet completed" });

            var patient = await _context.Users.FindAsync(order.PatientUserId);
            if (patient == null || string.IsNullOrEmpty(patient.Email))
                return BadRequest(new { success = false, message = "Patient email not found" });

            await _emailService.SendLabReportAsync(
                patient.Email,
                patient.FullName,
                order.TestType,
                order.ResultNotes ?? "See attached report",
                order.CompletedAt?.ToString("dd MMM yyyy") ?? DateTime.UtcNow.ToString("dd MMM yyyy"),
                order.ReportUrl ?? ""
            );

            return Ok(new { success = true, message = "Lab report sent to patient's email" });
        }

        public class CompleteOrderDto
        {
            public string? ResultNotes { get; set; }
            public string? ReportUrl { get; set; }
        }
    }
}
