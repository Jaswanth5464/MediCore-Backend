using MediCore.API.Infrastructure.Database.Context;
using MediCore.API.Modules.Laboratory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCore.API.Modules.Laboratory.Controllers
{
    [ApiController]
    [Route("api/lab")]
    [Authorize(Roles = "SuperAdmin,HospitalAdmin,LabTechnician,Doctor")]
    public class LabController : ControllerBase
    {
        private readonly MediCoreDbContext _context;

        public LabController(MediCoreDbContext context)
        {
            _context = context;
        }

        [HttpGet("tests")]
        public async Task<IActionResult> GetTestMasters()
        {
            var tests = await _context.LabTestMasters
                .Where(t => t.IsActive)
                .OrderBy(t => t.TestName)
                .ToListAsync();
            return Ok(new { success = true, data = tests });
        }

        [HttpGet("queue")]
        public async Task<IActionResult> GetQueue()
        {
            var orders = await _context.LabOrders
                .Include(o => o.Appointment)
                .ThenInclude(a => a.PatientUser)
                .Include(o => o.Appointment)
                .ThenInclude(a => a.DoctorProfile)
                .ThenInclude(d => d.User)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new {
                    id = o.Id,
                    appointmentId = o.AppointmentId,
                    patientName = o.Appointment.PatientUser.FullName,
                    doctorName = o.Appointment.DoctorProfile.User.FullName,
                    testType = o.TestType,
                    status = o.Status,
                    createdAt = o.CreatedAt,
                    reportUrl = o.ReportUrl,
                    criticalAlert = o.CriticalAlert
                })
                .ToListAsync();

            return Ok(new { success = true, data = orders });
        }

        [HttpPost("upload-report/{orderId}")]
        public async Task<IActionResult> UploadReport(int orderId, [FromBody] UploadReportRequest request)
        {
            var order = await _context.LabOrders.FindAsync(orderId);
            if (order == null) return NotFound(new { success = false, message = "Order not found" });

            // In reality, save base64 to blob storage. For MVP, store short text string or a fake URL.
            order.ReportUrl = request.ReportPdfUrl; 
            order.ResultNotes = request.ResultNotes;
            order.Status = "Completed";
            order.CompletedAt = DateTime.UtcNow;
            if (request.IsCritical) order.CriticalAlert = true;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Report uploaded successfully" });
        }
    }

    public class UploadReportRequest
    {
        public string ReportPdfUrl { get; set; } = string.Empty;
        public string? ResultNotes { get; set; }
        public bool IsCritical { get; set; }
    }
}
