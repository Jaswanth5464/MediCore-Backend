using MediCore.API.Infrastructure.Database.Context;
using MediCore.API.Modules.Finance.Models;
using MediCore.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MediCore.API.Modules.Finance.Controllers
{
    [ApiController]
    [Route("api/finance")]
    public class FinanceController : ControllerBase
    {
        private readonly MediCoreDbContext _context;
        private readonly IEmailService _emailService;

        public FinanceController(MediCoreDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET api/finance/my-bills  — patient sees their own bills only
        [HttpGet("my-bills")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetMyBills()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { success = false, message = "Invalid token" });

            var bills = await _context.Bills
                .Include(b => b.Appointment)
                .Where(b => b.Appointment.PatientUserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var doctorProfileIds = bills.Select(b => b.Appointment.DoctorProfileId).Distinct().ToList();
            var doctors = await _context.DoctorProfiles
                .Where(d => doctorProfileIds.Contains(d.Id))
                .Select(d => new
                {
                    d.Id,
                    FullName = _context.Users.Where(u => u.Id == d.UserId).Select(u => u.FullName).FirstOrDefault()
                })
                .ToDictionaryAsync(d => d.Id, d => d.FullName);

            var result = bills.Select(b => new
            {
                b.Id,
                b.BillNumber,
                b.AppointmentId,
                PatientUserId = userId,
                DoctorName = doctors.TryGetValue(b.Appointment.DoctorProfileId, out var dName) ? dName : "Unknown",
                b.Items,
                b.SubTotal,
                b.TotalAmount,
                b.Status,
                b.PaymentMode,
                b.CreatedAt
            });

            return Ok(new { success = true, data = result });
        }

        // GET api/finance/bills (staff only)
        [HttpGet("bills")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,FinanceStaff,Receptionist")]
        public async Task<IActionResult> GetBills([FromQuery] string? status = null)
        {
            var query = _context.Bills
                .Include(b => b.Appointment)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(b => b.Status == status);
            }

            var billsList = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
            
            // Manual fetch for names (since navigation properties are omitted in Appointment)
            var patientIds = billsList.Select(b => b.Appointment.PatientUserId).Distinct().ToList();
            var doctorProfileIds = billsList.Select(b => b.Appointment.DoctorProfileId).Distinct().ToList();
            
            var patients = await _context.Users.Where(u => patientIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => new { u.FullName, u.PhoneNumber });
            
            var doctors = await _context.DoctorProfiles
                .Where(d => doctorProfileIds.Contains(d.Id))
                .Select(d => new
                {
                    d.Id,
                    FullName = _context.Users.Where(u => u.Id == d.UserId).Select(u => u.FullName).FirstOrDefault()
                })
                .ToDictionaryAsync(d => d.Id, d => d.FullName);

            var result = billsList.Select(b => new
            {
                b.Id,
                b.BillNumber,
                b.AppointmentId,
                PatientName = patients.TryGetValue(b.Appointment.PatientUserId, out var p) ? p.FullName : "Unknown",
                PatientPhone = p?.PhoneNumber ?? "",
                DoctorName = doctors.TryGetValue(b.Appointment.DoctorProfileId, out var dName) ? dName : "Unknown",
                b.SubTotal,
                b.GSTAmount,
                b.Discount,
                b.TotalAmount,
                b.Status,
                b.PaymentMode,
                b.CreatedAt
            });

            return Ok(new { success = true, data = result });
        }

        // PATCH api/finance/bills/{id}/status
        [HttpPatch("bills/{id}/status")]
        public async Task<IActionResult> UpdateBillStatus(int id, [FromBody] UpdateBillDto dto)
        {
            var bill = await _context.Bills.FindAsync(id);
            if (bill == null) return NotFound(new { success = false, message = "Bill not found" });

            bill.Status = dto.Status;
            bill.PaymentMode = dto.PaymentMode;
            bill.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Bill updated successfully" });
        }

        // POST api/finance/bills/{id}/email
        [HttpPost("bills/{id}/email")]
        public async Task<IActionResult> EmailBill(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.Appointment)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null) return NotFound(new { success = false, message = "Bill not found" });

            var patient = await _context.Users.FindAsync(bill.Appointment.PatientUserId);
            if (patient == null || string.IsNullOrEmpty(patient.Email))
                return BadRequest(new { success = false, message = "Patient email not found" });

            await _emailService.SendInvoiceAsync(
                patient.Email,
                patient.FullName,
                bill.BillNumber,
                bill.TotalAmount,
                bill.CreatedAt.ToString("dd MMM yyyy"),
                bill.Status
            );

            return Ok(new { success = true, message = "Invoice sent to patient's email" });
        }

        public class UpdateBillDto
        {
            public string Status { get; set; } = string.Empty;
            public string? PaymentMode { get; set; }
        }
    }
}
