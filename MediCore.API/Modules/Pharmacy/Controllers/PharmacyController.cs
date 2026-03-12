using MediCore.API.Infrastructure.Database.Context;
using MediCore.API.Modules.Pharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCore.API.Modules.Pharmacy.Controllers
{
    [ApiController]
    [Route("api/pharmacy")]
    [Authorize(Roles = "SuperAdmin,HospitalAdmin,Pharmacist")]
    public class PharmacyController : ControllerBase
    {
        private readonly MediCoreDbContext _context;

        public PharmacyController(MediCoreDbContext context)
        {
            _context = context;
        }

        // GET api/pharmacy/inventory
        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventory()
        {
            var medicines = await _context.Medicines
                .OrderBy(m => m.Name)
                .ToListAsync();
            return Ok(new { success = true, data = medicines });
        }

        // POST api/pharmacy/inventory
        [HttpPost("inventory")]
        public async Task<IActionResult> AddMedicine([FromBody] Medicine medicine)
        {
            _context.Medicines.Add(medicine);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = medicine, message = "Medicine added successfully" });
        }

        // GET api/pharmacy/queue
        [HttpGet("queue")]
        public async Task<IActionResult> GetDispensingQueue()
        {
            var today = DateTime.UtcNow.Date;
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Appointment)
                .ThenInclude(a => a.PatientUser)
                .Include(p => p.Appointment)
                .ThenInclude(a => a.DoctorProfile)
                .ThenInclude(d => d.User)
                .Where(p => !p.IsDispensed && p.CreatedAt >= today.AddDays(-3)) // Last 3 days
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new {
                    id = p.Id,
                    appointmentId = p.AppointmentId,
                    patientName = p.Appointment.PatientUser.FullName,
                    doctorName = p.Appointment.DoctorProfile.User.FullName,
                    medicinesJson = p.MedicinesJson,
                    advice = p.Advice,
                    createdAt = p.CreatedAt,
                    isDispensed = p.IsDispensed
                })
                .ToListAsync();

            return Ok(new { success = true, data = prescriptions });
        }

        // POST api/pharmacy/dispense/{prescriptionId}
        [HttpPost("dispense/{prescriptionId}")]
        public async Task<IActionResult> DispensePrescription(int prescriptionId)
        {
            var prescription = await _context.Prescriptions.FindAsync(prescriptionId);
            if (prescription == null) return NotFound(new { success = false, message = "Prescription not found" });

            if (prescription.IsDispensed) return BadRequest(new { success = false, message = "Already dispensed" });

            // Ideally parse medicinesJson and deduct stock. For MVP, we will just mark dispensed.
            prescription.IsDispensed = true;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Medicines dispensed successfully" });
        }
    }
}
