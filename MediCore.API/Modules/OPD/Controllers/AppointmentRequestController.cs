using MediCore.API.Infrastructure.Database.Context;
using MediCore.API.Modules.OPD.Models;
using MediCore.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCore.API.Modules.OPD.Controllers
{
    [ApiController]
    [Route("api/appointment-requests")]
    public class AppointmentRequestController : ControllerBase
    {
        private readonly MediCoreDbContext _context;
        private readonly IEmailService _emailService;

        public AppointmentRequestController(
            MediCoreDbContext context,
            IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ── PUBLIC — No Auth needed ──────────────────

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { success = false, message = "Email address is required." });

            // Generate 6-digit OTP
            var otpCode = new Random().Next(100000, 999999).ToString();

            // Invalidate any active OTPs for this number
            var existingRecords = await _context.OtpRecords
                .Where(o => o.Email == dto.Email && !o.IsUsed && o.ExpiryTime > DateTime.UtcNow)
                .ToListAsync();
            
            foreach (var record in existingRecords)
            {
                record.IsUsed = true;
            }

            var otpRecord = new OtpRecord
            {
                Email = dto.Email,
                OtpCode = otpCode,
                ExpiryTime = DateTime.UtcNow.AddMinutes(5), // 5 min expiry
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.OtpRecords.Add(otpRecord);
            await _context.SaveChangesAsync();

            // Send standard SMTP email
            await _emailService.SendOtpEmailAsync(dto.Email, otpCode);

            return Ok(new 
            { 
                success = true, 
                message = "Verification code sent to email."
            });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Otp))
                return BadRequest(new { success = false, message = "Email and OTP are required." });

            var validRecord = await _context.OtpRecords
                .Where(o => o.Email == dto.Email 
                         && o.OtpCode == dto.Otp 
                         && !o.IsUsed 
                         && o.ExpiryTime > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (validRecord == null)
                return BadRequest(new { success = false, message = "Invalid or expired OTP." });

            validRecord.IsUsed = true;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Email address verified successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitRequest([FromBody] SubmitRequestDto dto)
        {
            try
            {
                var today = DateTime.UtcNow.ToString("yyyyMMdd");
                var count = await _context.AppointmentRequests
                    .CountAsync(r => r.CreatedAt.Date == DateTime.UtcNow.Date);
                var refNumber = $"REQ{today}{(count + 1):D3}";

                var request = new AppointmentRequest
                {
                    ReferenceNumber = refNumber,
                    FullName = dto.FullName,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    Age = dto.Age,
                    Gender = dto.Gender,
                    PreferredDepartmentId = dto.PreferredDepartmentId,
                    Symptoms = dto.Symptoms,
                    PreferredDate = dto.PreferredDate,
                    VisitType = dto.VisitType ?? "Consultation",
                    IsFirstVisit = dto.IsFirstVisit,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.AppointmentRequests.Add(request);
                await _context.SaveChangesAsync();

                // Send confirmation email to patient
                _ = Task.Run(async () =>
                {
                    await _emailService.SendAppointmentRequestConfirmationAsync(
                        dto.Email,
                        dto.FullName,
                        refNumber
                    );
                });

                return Ok(new
                {
                    success = true,
                    message = "Appointment request submitted successfully",
                    data = new
                    {
                        referenceNumber = refNumber,
                        message = "Our team will contact you within 2 hours",
                        estimatedCallback = "Within 2 hours"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("track/{referenceNumber}")]
        public async Task<IActionResult> TrackRequest(string referenceNumber)
        {
            var request = await _context.AppointmentRequests
                .FirstOrDefaultAsync(r => r.ReferenceNumber == referenceNumber);

            if (request == null)
                return NotFound(new { success = false, message = "Reference number not found" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    request.ReferenceNumber,
                    request.FullName,
                    request.Status,
                    request.CreatedAt,
                    StatusMessage = GetStatusMessage(request.Status),
                    request.RejectionReason
                }
            });
        }

        // ── RECEPTIONIST — Auth required ─────────────

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,Receptionist")]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            var query = _context.AppointmentRequests.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.ReferenceNumber,
                    r.FullName,
                    r.Phone,
                    r.Email,
                    r.Age,
                    r.Gender,
                    r.Symptoms,
                    r.VisitType,
                    r.IsFirstVisit,
                    r.PreferredDate,
                    r.Status,
                    r.CreatedAt,
                    r.ReceptionistNotes,
                    PreferredDepartment = r.PreferredDepartmentId != null
                        ? _context.Departments
                            .Where(d => d.Id == r.PreferredDepartmentId)
                            .Select(d => d.Name)
                            .FirstOrDefault()
                        : "No preference",
                    MinutesSinceSubmitted = (int)(DateTime.UtcNow - r.CreatedAt).TotalMinutes
                })
                .ToListAsync();

            var summary = new
            {
                Total = requests.Count,
                Pending = requests.Count(r => r.Status == "Pending"),
                Contacted = requests.Count(r => r.Status == "Contacted"),
                Confirmed = requests.Count(r => r.Status == "Confirmed"),
                Rejected = requests.Count(r => r.Status == "Rejected")
            };

            return Ok(new { success = true, data = requests, summary });
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,Receptionist")]
        public async Task<IActionResult> GetById(int id)
        {
            var request = await _context.AppointmentRequests
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound(new { success = false, message = "Request not found" });

            return Ok(new { success = true, data = request });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,Receptionist")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var request = await _context.AppointmentRequests.FindAsync(id);
            if (request == null)
                return NotFound(new { success = false, message = "Request not found" });

            _context.AppointmentRequests.Remove(request);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Request deleted successfully" });
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,Receptionist")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var request = await _context.AppointmentRequests.FindAsync(id);
            if (request == null)
                return NotFound(new { success = false, message = "Request not found" });

            request.Status = dto.Status;
            request.ReceptionistNotes = dto.Notes;
            request.RejectionReason = dto.RejectionReason;
            request.HandledAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Status updated to {dto.Status}" });
        }

        [HttpPost("{id}/convert")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,Receptionist")]
        public async Task<IActionResult> ConvertToAppointment(
            int id, [FromBody] ConvertToAppointmentDto dto)
        {
            try
            {
                var request = await _context.AppointmentRequests.FindAsync(id);
                if (request == null)
                    return NotFound(new { success = false, message = "Request not found" });

                if (request.Status == "Converted")
                    return BadRequest(new { success = false, message = "Already converted" });

                // Check if patient user exists by email
                var patientUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                string tempPassword = "";
                bool isNewAccount = false;

                // If no account exists create one
                if (patientUser == null)
                {
                    tempPassword = $"Med@{new Random().Next(1000, 9999)}";
                    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(tempPassword);

                    patientUser = new Auth.Models.User
                    {
                        FullName = request.FullName,
                        Email = request.Email,
                        PhoneNumber = request.Phone,
                        PasswordHash = hashedPassword,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UserRoles = new List<Auth.Models.UserRole> { new Auth.Models.UserRole { RoleId = 10 } }
                    };

                    _context.Users.Add(patientUser);
                    await _context.SaveChangesAsync();
                    isNewAccount = true;
                }

                // Get doctor profile for fee
                var doctorProfile = await _context.DoctorProfiles
                    .FindAsync(dto.DoctorProfileId);

                if (doctorProfile == null)
                    return BadRequest(new { success = false, message = "Doctor profile not found" });

                // Get doctor name
                var doctorName = await _context.Users
                    .Where(u => u.Id == doctorProfile.UserId)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync() ?? "Doctor";

                // Generate token number
                var tokenCount = await _context.Appointments
                    .CountAsync(a => a.AppointmentDate.Date == dto.AppointmentDate.Date
                        && a.DoctorProfileId == dto.DoctorProfileId);
                var tokenNumber = $"OPD{dto.AppointmentDate:yyyyMMdd}{(tokenCount + 1):D3}";

                // Create appointment
                var appointment = new Appointment
                {
                    TokenNumber = tokenNumber,
                    PatientUserId = patientUser.Id,
                    DoctorProfileId = dto.DoctorProfileId,
                    DepartmentId = dto.DepartmentId,
                    AppointmentDate = dto.AppointmentDate,
                    TimeSlot = TimeSpan.Parse(dto.TimeSlot),
                    VisitType = request.VisitType,
                    Symptoms = request.Symptoms,
                    IsFirstVisit = request.IsFirstVisit,
                    Status = "Scheduled",
                    ConsultationFee = doctorProfile.ConsultationFee,
                    PaymentStatus = "Pending",
                    AppointmentRequestId = request.Id,
                    ReceptionistNotes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Appointments.Add(appointment);

                request.Status = "Converted";
                request.HandledAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send confirmed appointment email with credentials
                _ = Task.Run(async () =>
                {
                    await _emailService.SendAppointmentConfirmedAsync(
                        request.Email,
                        request.FullName,
                        doctorName,
                        appointment.AppointmentDate.ToString("dd MMM yyyy"),
                        DateTime.Today.Add(appointment.TimeSlot).ToString("hh:mm tt"),
                        appointment.TokenNumber,
                        appointment.ConsultationFee,
                        request.Email,
                        tempPassword
                    );
                });

                return Ok(new
                {
                    success = true,
                    message = "Appointment created successfully",
                    data = new
                    {
                        appointment.Id,
                        appointment.TokenNumber,
                        PatientEmail = request.Email,
                        PatientPhone = request.Phone,
                        IsNewAccount = isNewAccount,
                        TempPassword = tempPassword,
                        appointment.AppointmentDate,
                        appointment.ConsultationFee,
                        DoctorName = doctorName
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

        private static string GetStatusMessage(string status) => status switch
        {
            "Pending" => "Your request is received. Our team will contact you within 2 hours.",
            "Contacted" => "Our team has contacted you. Please check your phone.",
            "Confirmed" => "Your appointment is confirmed. Check your email for details.",
            "Rejected" => "Unfortunately we could not process your request.",
            "Converted" => "Your appointment is booked. Please check your email for login details.",
            _ => "Status unknown"
        };
    }

    // ── DTOs ─────────────────────────────────────────

    public class SubmitRequestDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public int? PreferredDepartmentId { get; set; }
        public string Symptoms { get; set; } = string.Empty;
        public DateTime? PreferredDate { get; set; }
        public string? VisitType { get; set; }
        public bool IsFirstVisit { get; set; } = true;
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class SendOtpDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyOtpDto
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    public class ConvertToAppointmentDto
    {
        public int DoctorProfileId { get; set; }
        public int DepartmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string TimeSlot { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? TempPassword { get; set; }
        public bool IsNewAccount { get; set; }
    }
}
