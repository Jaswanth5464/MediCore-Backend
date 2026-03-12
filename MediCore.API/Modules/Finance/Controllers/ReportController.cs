using MediCore.API.Infrastructure.Database.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCore.API.Modules.Finance.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly MediCoreDbContext _context;

        public ReportController(MediCoreDbContext context)
        {
            _context = context;
        }

        [HttpGet("revenue-summary")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin")]
        public async Task<IActionResult> GetRevenueSummary()
        {
            var last30Days = DateTime.UtcNow.Date.AddDays(-30);
            
            var revenueByDateData = await _context.Bills
                .Where(b => b.Status == "Paid" && b.CreatedAt >= last30Days)
                .GroupBy(b => b.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Amount = g.Sum(b => b.TotalAmount) })
                .OrderBy(g => g.Date)
                .ToListAsync();

            var revenueByDate = revenueByDateData
                .Select(g => new { Date = g.Date.ToString("yyyy-MM-dd"), Amount = g.Amount })
                .ToList();

            var totalRevenue = await _context.Bills
                .Where(b => b.Status == "Paid")
                .SumAsync(b => b.TotalAmount);

            var revenueByDept = await _context.Bills
                .Where(b => b.Status == "Paid")
                .Include(b => b.Appointment)
                .ThenInclude(a => a.Department)
                .GroupBy(b => b.Appointment.Department.Name)
                .Select(g => new { Department = g.Key, Amount = g.Sum(b => b.TotalAmount) })
                .ToListAsync();

            return Ok(new { 
                success = true, 
                data = new {
                    totalRevenue,
                    revenueByDate,
                    revenueByDept
                }
            });
        }

        [HttpGet("system-stats")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin")]
        public async Task<IActionResult> GetSystemStats()
        {
            var totalPatients = await _context.UserRoles.CountAsync(ur => ur.Role.Name == "Patient");
            var totalDoctors = await _context.UserRoles.CountAsync(ur => ur.Role.Name == "Doctor");
            var totalAppointments = await _context.Appointments.CountAsync();
            var pendingRequests = await _context.AppointmentRequests.CountAsync(r => r.Status == "Pending");

            return Ok(new {
                success = true,
                data = new {
                    totalPatients,
                    totalDoctors,
                    totalAppointments,
                    pendingRequests
                }
            });
        }

        [HttpGet("audit-logs")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin")]
        public async Task<IActionResult> GetAuditLogs()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .ToListAsync();

            return Ok(new { success = true, data = logs });
        }

        [HttpGet("doctor-performance/{doctorId}")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,Doctor")]
        public async Task<IActionResult> GetDoctorPerformance(int doctorId)
        {
            var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            
            var appointments = await _context.Appointments
                .Where(a => a.DoctorProfileId == doctorId && a.Status == "Completed" && a.AppointmentDate >= firstDayOfMonth)
                .ToListAsync();

            var patientsThisMonth = appointments.Count;
            
            var revenueThisMonth = await _context.Bills
                .Where(b => b.DoctorProfileId == doctorId && b.CreatedAt >= firstDayOfMonth && b.Status == "Paid")
                .SumAsync(b => b.TotalAmount);

            double avgConsultationMinutes = 0;
            var timedAppointments = appointments
                .Where(a => a.ConsultationStartedAt.HasValue && a.CompletedAt.HasValue)
                .ToList();

            if (timedAppointments.Any())
            {
                avgConsultationMinutes = timedAppointments
                    .Average(a => (a.CompletedAt.Value - a.ConsultationStartedAt.Value).TotalMinutes);
            }

            return Ok(new {
                success = true,
                data = new {
                    patientsThisMonth,
                    revenueThisMonth,
                    avgConsultationMinutes = Math.Round(avgConsultationMinutes, 1),
                    totalCompleted = await _context.Appointments.CountAsync(a => a.DoctorProfileId == doctorId && a.Status == "Completed")
                }
            });
        }
    }
}
