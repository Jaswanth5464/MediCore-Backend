using MediCore.API.Infrastructure.Database.Context;
using MediCore.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MediCore.API.Modules.Finance.Controllers
{
    [ApiController]
    [Route("api/digest")]
    [Authorize(Roles = "SuperAdmin,HospitalAdmin")]
    public class DigestController : ControllerBase
    {
        private readonly MediCoreDbContext _context;
        private readonly IEmailService _emailService;

        public DigestController(MediCoreDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // POST api/digest/send
        // Admin clicks button → computes today's stats → sends email digest
        [HttpPost("send")]
        public async Task<IActionResult> SendDigest([FromBody] SendDigestDto dto)
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                          ?? User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var adminName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                         ?? User.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                         ?? "Admin";

            if (string.IsNullOrEmpty(adminEmail))
                return BadRequest(new { success = false, message = "Could not determine admin email from token" });

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            // Count patients seen today (Completed or CheckedIn)
            var totalPatients = await _context.Appointments
                .CountAsync(a => a.AppointmentDate.Date == today && (a.Status == "Completed" || a.Status == "CheckedIn" || a.Status == "WithDoctor"));

            // Revenue today (from paid bills)
            var totalRevenue = await _context.Bills
                .Where(b => b.CreatedAt.Date == today)
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

            // Pending appointments (Scheduled but not yet checked in)
            var pendingAppts = await _context.Appointments
                .CountAsync(a => a.AppointmentDate.Date == today && a.Status == "Scheduled");

            // Tomorrow's bookings
            var tomorrowCount = await _context.Appointments
                .CountAsync(a => a.AppointmentDate.Date == tomorrow && a.Status != "Cancelled");

            try
            {
                await _emailService.SendDailyDigestAsync(
                    adminEmail,
                    adminName,
                    totalPatients,
                    totalRevenue,
                    pendingAppts,
                    tomorrowCount,
                    dto?.DietPlanHtml ?? ""
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Email failed: {ex.Message}"
                });
            }

            return Ok(new
            {
                success = true,
                message = $"Daily digest sent to {adminEmail}",
                data = new { totalPatients, totalRevenue, pendingAppts, tomorrowCount }
            });
        }
    }

    public class SendDigestDto
    {
        public string? DietPlanHtml { get; set; }
    }
}
