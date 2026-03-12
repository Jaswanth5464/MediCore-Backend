using MediCore.API.Infrastructure.Database.Context;
using MediCore.API.Modules.Laboratory.Models;
using MediCore.API.Modules.OPD.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCore.API.Modules.OPD.Controllers
{
    [ApiController]
    [Route("api/consultation")]
    [Authorize(Roles = "SuperAdmin,HospitalAdmin,Doctor,Nurse,Receptionist")]
    public class ConsultationController : ControllerBase
    {
        private readonly MediCoreDbContext _context;

        public ConsultationController(MediCoreDbContext context)
        {
            _context = context;
        }

        // GET api/consultation/{appointmentId}
        [HttpGet("{appointmentId}")]
        public async Task<IActionResult> GetConsultationData(int appointmentId)
        {
            var vitals = await _context.Vitals.Where(v => v.AppointmentId == appointmentId).ToListAsync();
            var prescriptions = await _context.Prescriptions.Where(p => p.AppointmentId == appointmentId).ToListAsync();
            var labOrders = await _context.LabOrders.Where(l => l.AppointmentId == appointmentId).ToListAsync();

            return Ok(new
            {
                success = true,
                data = new { vitals, prescriptions, labOrders }
            });
        }

        // POST api/consultation/vitals
        [HttpPost("vitals")]
        public async Task<IActionResult> SaveVitals([FromBody] Vitals vitals)
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            if (userIdStr != null && int.TryParse(userIdStr, out int userId))
            {
                vitals.RecordedById = userId;
            }

            vitals.RecordedAt = DateTime.UtcNow;
            _context.Vitals.Add(vitals);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = vitals, message = "Vitals saved successfully" });
        }

        // POST api/consultation/prescription
        [HttpPost("prescription")]
        public async Task<IActionResult> SavePrescription([FromBody] Prescription prescription)
        {
            prescription.CreatedAt = DateTime.UtcNow;
            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = prescription, message = "Prescription saved successfully" });
        }

        // POST api/consultation/lab-orders
        [HttpPost("lab-orders")]
        public async Task<IActionResult> SaveLabOrder([FromBody] LabOrder order)
        {
            order.CreatedAt = DateTime.UtcNow;
            order.Status = "Pending";
            _context.LabOrders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = order, message = "Lab order sent successfully" });
        }
    }
}
