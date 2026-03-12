using MediCore.API.Infrastructure.Database.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore.Migrations;
using MediCore.API.Services;

namespace MediCore.API.Modules.OPD.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    public class AppointmentController : ControllerBase
    {
        private readonly MediCoreDbContext _context;

        public AppointmentController(MediCoreDbContext context)
        {
            _context = context;
        }

        // GET api/appointments/today
        [HttpGet("today")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,Receptionist,Doctor,Nurse")]
        public async Task<IActionResult> GetToday([FromQuery] int? doctorProfileId = null)
        {
            var today = DateTime.UtcNow.Date;
            var query = _context.Appointments
                .Where(a => a.AppointmentDate.Date == today);

            if (doctorProfileId.HasValue)
                query = query.Where(a => a.DoctorProfileId == doctorProfileId.Value);

            var appointments = await query
                .OrderBy(a => a.TimeSlot)
                .Select(a => new
                {
                    a.Id,
                    a.TokenNumber,
                    a.Status,
                    a.TimeSlot,
                    a.VisitType,
                    a.Symptoms,
                    a.ConsultationFee,
                    a.PaymentStatus,
                    a.IsFirstVisit,
                    Patient = _context.Users
                        .Where(u => u.Id == a.PatientUserId)
                        .Select(u => new { u.Id, u.FullName, u.PhoneNumber, u.Email })
                        .FirstOrDefault(),
                    Doctor = _context.DoctorProfiles
                        .Where(d => d.Id == a.DoctorProfileId)
                        .Select(d => new
                        {
                            d.Id,
                            d.Specialization,
                            FullName = _context.Users
                                .Where(u => u.Id == d.UserId)
                                .Select(u => u.FullName)
                                .FirstOrDefault()
                        })
                        .FirstOrDefault(),
                    Department = _context.Departments
                        .Where(d => d.Id == a.DepartmentId)
                        .Select(d => new { d.Id, d.Name, d.Icon })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = appointments,
                summary = new
                {
                    Total = appointments.Count,
                    Scheduled = appointments.Count(a => a.Status == "Scheduled"),
                    CheckedIn = appointments.Count(a => a.Status == "CheckedIn"),
                    WithDoctor = appointments.Count(a => a.Status == "WithDoctor"),
                    Completed = appointments.Count(a => a.Status == "Completed"),
                    Cancelled = appointments.Count(a => a.Status == "Cancelled")
                }
            });
        }

        // GET api/appointments/live-tokens
        [HttpGet("live-tokens")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLiveTokens()
        {
            var today = DateTime.UtcNow.Date;
            var activeAppointments = await _context.Appointments
                .Where(a => a.AppointmentDate.Date == today && (a.Status == "CheckedIn" || a.Status == "WithDoctor" || a.Status == "Scheduled"))
                .OrderBy(a => a.TimeSlot)
                .Select(a => new
                {
                    a.TokenNumber,
                    a.Status,
                    a.TimeSlot,
                    Department = _context.Departments
                        .Where(d => d.Id == a.DepartmentId)
                        .Select(d => d.Name)
                        .FirstOrDefault(),
                    Doctor = _context.DoctorProfiles
                        .Where(d => d.Id == a.DoctorProfileId)
                        .Select(d => _context.Users.FirstOrDefault(u => u.Id == d.UserId)!.FullName)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(new { success = true, data = activeAppointments });
        }

        // GET api/appointments/my
        [HttpGet("my")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetMyAppointments()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr == null || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { success = false, message = "Invalid token" });

            var today = DateTime.UtcNow.Date;
            var appointments = await _context.Appointments
                .Where(a => a.PatientUserId == userId)
                .OrderBy(a => a.AppointmentDate).ThenBy(a => a.TimeSlot)
                .Select(a => new
                {
                    a.Id,
                    a.TokenNumber,
                    a.Status,
                    a.AppointmentDate,
                    a.TimeSlot,
                    a.VisitType,
                    a.ConsultationFee,
                    a.PaymentStatus,
                    a.Symptoms,
                    DoctorName = _context.DoctorProfiles
                        .Where(d => d.Id == a.DoctorProfileId)
                        .Select(d => _context.Users.FirstOrDefault(u => u.Id == d.UserId)!.FullName)
                        .FirstOrDefault(),
                    DepartmentName = _context.Departments
                        .Where(d => d.Id == a.DepartmentId)
                        .Select(d => d.Name)
                        .FirstOrDefault(),
                    Doctor = _context.DoctorProfiles
                        .Where(d => d.Id == a.DoctorProfileId)
                        .Select(d => new
                        {
                            d.Specialization,
                            d.Qualification,
                            FullName = _context.Users
                                .Where(u => u.Id == d.UserId)
                                .Select(u => u.FullName)
                                .FirstOrDefault()
                        })
                        .FirstOrDefault(),
                    Department = _context.Departments
                        .Where(d => d.Id == a.DepartmentId)
                        .Select(d => new { d.Name, d.Icon })
                        .FirstOrDefault()
                })
                .ToListAsync();

            var upcoming = appointments.Where(a => a.AppointmentDate.Date >= today && a.Status != "Cancelled" && a.Status != "Completed").ToList();
            var past = appointments.Where(a => a.AppointmentDate.Date < today || a.Status == "Completed").ToList();

            return Ok(new { success = true, data = new { upcoming, past } });
        }

        // GET api/appointments
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,Receptionist,Doctor,Nurse")]
        public async Task<IActionResult> GetAll(
            [FromQuery] DateTime? date = null,
            [FromQuery] int? doctorProfileId = null,
            [FromQuery] string? status = null)
        {
            var query = _context.Appointments.AsQueryable();

            if (date.HasValue)
                query = query.Where(a => a.AppointmentDate.Date == date.Value.Date);
            
            if (doctorProfileId.HasValue)
                query = query.Where(a => a.DoctorProfileId == doctorProfileId.Value);

            if (!string.IsNullOrEmpty(status) && status != "All")
                query = query.Where(a => a.Status == status);

            var appointments = await query
                .OrderBy(a => a.TimeSlot)
                .Select(a => new
                {
                    a.Id,
                    a.TokenNumber,
                    a.Status,
                    a.TimeSlot,
                    a.VisitType,
                    a.Symptoms,
                    a.ConsultationFee,
                    a.PaymentStatus,
                    a.IsFirstVisit,
                    a.AppointmentDate,
                    Patient = _context.Users
                        .Where(u => u.Id == a.PatientUserId)
                        .Select(u => new { u.Id, u.FullName, u.PhoneNumber, u.Email })
                        .FirstOrDefault(),
                    Doctor = _context.DoctorProfiles
                        .Where(d => d.Id == a.DoctorProfileId)
                        .Select(d => new
                        {
                            d.Id,
                            d.Specialization,
                            FullName = _context.Users
                                .Where(u => u.Id == d.UserId)
                                .Select(u => u.FullName)
                                .FirstOrDefault()
                        })
                        .FirstOrDefault(),
                    Department = _context.Departments
                        .Where(d => d.Id == a.DepartmentId)
                        .Select(d => new { d.Id, d.Name, d.Icon })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(new { success = true, data = appointments });
        }


        // PATCH api/appointments/5/status
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,Receptionist,Doctor,Nurse")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateAppointmentStatusDto dto)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound(new { success = false, message = "Appointment not found" });

            var now = DateTime.UtcNow;
            var prevStatus = appointment.Status;
            appointment.Status = dto.Status;
            appointment.UpdatedAt = now;

            if (dto.DoctorNotes != null)
                appointment.DoctorNotes = dto.DoctorNotes;

            // State Machine Side Effects
            if (dto.Status == "CheckedIn" && appointment.CheckedInAt == null)
            {
                appointment.CheckedInAt = now;
                // Calculate queue position among checked-in patients for this doctor/date
                var position = await _context.Appointments
                    .Where(a => a.DoctorProfileId == appointment.DoctorProfileId
                        && a.AppointmentDate.Date == appointment.AppointmentDate.Date
                        && (a.Status == "CheckedIn" || a.Status == "WithDoctor"))
                    .CountAsync();
                appointment.QueuePosition = position + 1;
            }
            else if (dto.Status == "WithDoctor" && appointment.ConsultationStartedAt == null)
            {
                appointment.ConsultationStartedAt = now;
            }
            else if (dto.Status == "Completed" && appointment.CompletedAt == null)
            {
                appointment.CompletedAt = now;
                
                // Fetch associated consultation data to calculate total bill
                var labOrders = await _context.LabOrders.Where(l => l.AppointmentId == id).ToListAsync();
                var prescriptions = await _context.Prescriptions.Where(p => p.AppointmentId == id).ToListAsync();
                
                var items = new List<object>();
                var fee = appointment.ConsultationFee > 0 ? appointment.ConsultationFee : 500;
                
                items.Add(new { description = "Consultation Fee", amount = fee });
                var total = fee;

                // Add Lab Tests to Bill (assume flat 800 per test for demo purposes if not specified)
                foreach(var lab in labOrders)
                {
                    decimal labCost = 800; 
                    items.Add(new { description = $"Lab Test: {lab.TestType}", amount = labCost });
                    total += labCost;
                }

                // Add Medicines to Bill (assume flat 150 per prescription for demo purposes)
                foreach(var presc in prescriptions)
                {
                    decimal prescCost = 150;
                    items.Add(new { description = "Pharmacy Medicines", amount = prescCost });
                    total += prescCost;
                }

                // Auto-create bill
                var billCount = await _context.Bills.CountAsync(b => b.CreatedAt.Date == now.Date);
                var billNumber = $"BILL{now:yyyyMMdd}{(billCount + 1):D3}";
                
                var bill = new MediCore.API.Modules.Finance.Models.Bill
                {
                    BillNumber = billNumber,
                    AppointmentId = appointment.Id,
                    PatientUserId = appointment.PatientUserId,
                    DoctorProfileId = appointment.DoctorProfileId,
                    Items = System.Text.Json.JsonSerializer.Serialize(items),
                    SubTotal = total,
                    TotalAmount = total,
                    Status = "Unpaid",
                    CreatedAt = now
                };
                _context.Bills.Add(bill);
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = $"Status updated to {dto.Status}" });
        }

        // PATCH api/appointments/5/payment
        [HttpPatch("{id}/payment")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,Receptionist,Patient")]
        public async Task<IActionResult> UpdatePayment(int id, [FromBody] UpdatePaymentDto dto)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound(new { success = false, message = "Appointment not found" });

            appointment.PaymentStatus = "Paid";
            appointment.PaymentMode = dto.PaymentMode;
            appointment.RazorpayPaymentId = dto.RazorpayPaymentId;
            appointment.UpdatedAt = DateTime.UtcNow;

            // Also mark the corresponding bill as paid
            var bill = await _context.Bills.FirstOrDefaultAsync(b => b.AppointmentId == id);
            if (bill == null)
            {
                // If no bill exists yet (e.g. appointment not Completed), create one for the consultation fee
                var now = DateTime.UtcNow;
                var billCount = await _context.Bills.CountAsync(b => b.CreatedAt.Date == now.Date);
                var billNumber = $"BILL{now:yyyyMMdd}{(billCount + 1):D3}";
                
                var items = new List<object> { new { description = "Consultation Fee", amount = appointment.ConsultationFee } };
                
                bill = new MediCore.API.Modules.Finance.Models.Bill
                {
                    BillNumber = billNumber,
                    AppointmentId = appointment.Id,
                    PatientUserId = appointment.PatientUserId,
                    DoctorProfileId = appointment.DoctorProfileId,
                    Items = System.Text.Json.JsonSerializer.Serialize(items),
                    SubTotal = appointment.ConsultationFee,
                    TotalAmount = appointment.ConsultationFee,
                    Status = "Paid",
                    PaymentMode = dto.PaymentMode,
                    PaidAt = now,
                    CreatedAt = now
                };
                _context.Bills.Add(bill);
            }
            else
            {
                bill.Status = "Paid";
                bill.PaymentMode = dto.PaymentMode;
                bill.PaidAt = DateTime.UtcNow;
                bill.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Payment recorded and bill generated" });
        }

        // POST api/appointments/5/request-offline-payment
        [HttpPost("{id}/request-offline-payment")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> RequestOfflinePayment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound(new { success = false, message = "Appointment not found" });

            appointment.PaymentStatus = "PendingOffline";
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Offline payment request sent to reception" });
        }

        // GET api/appointments/pending-payments
        [HttpGet("pending-payments")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,Receptionist")]
        public async Task<IActionResult> GetPendingPayments()
        {
            var pending = await _context.Appointments
                .Where(a => a.PaymentStatus == "PendingOffline")
                .OrderByDescending(a => a.UpdatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.TokenNumber,
                    a.AppointmentDate,
                    a.TimeSlot,
                    a.ConsultationFee,
                    PatientName = _context.Users.Where(u => u.Id == a.PatientUserId).Select(u => u.FullName).FirstOrDefault(),
                    DoctorName = _context.DoctorProfiles.Where(d => d.Id == a.DoctorProfileId).Select(d => _context.Users.Where(u => u.Id == d.UserId).Select(u => u.FullName).FirstOrDefault()).FirstOrDefault(),
                    DepartmentName = _context.Departments.Where(d => d.Id == a.DepartmentId).Select(d => d.Name).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(new { success = true, data = pending });
        }

        // GET api/appointments/calendar?date=2026-03-10&departmentId=1
        // The UNIFIED calendar endpoint — single source of truth for frontend calendar
        [HttpGet("calendar")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,Receptionist,Doctor,Nurse")]
        public async Task<IActionResult> GetCalendar(
            [FromQuery] DateTime date,
            [FromQuery] int? departmentId = null,
            [FromQuery] int? doctorProfileId = null)
        {
            var dateOnly = date.Date;

            // Fetch all appointments for this date
            var appointmentsQuery = _context.Appointments
                .Where(a => a.AppointmentDate.Date == dateOnly);

            if (departmentId.HasValue)
                appointmentsQuery = appointmentsQuery.Where(a => a.DepartmentId == departmentId.Value);
            if (doctorProfileId.HasValue)
                appointmentsQuery = appointmentsQuery.Where(a => a.DoctorProfileId == doctorProfileId.Value);

            var appointments = await appointmentsQuery.ToListAsync();

            // Get all relevant doctors
            var doctorQuery = _context.DoctorProfiles.Where(d => d.IsActive);
            if (departmentId.HasValue)
                doctorQuery = doctorQuery.Where(d => d.DepartmentId == departmentId.Value);
            if (doctorProfileId.HasValue)
                doctorQuery = doctorQuery.Where(d => d.Id == doctorProfileId.Value);

            var doctors = await doctorQuery.ToListAsync();

            var dayName = dateOnly.DayOfWeek.ToString()[..3]; // Mon, Tue, etc.
            var result = new List<object>();

            foreach (var doctor in doctors)
            {
                // Get doctor's full name
                var docUser = await _context.Users.FindAsync(doctor.UserId);
                var department = await _context.Departments.FindAsync(doctor.DepartmentId);

                if (!doctor.AvailableDays.Contains(dayName))
                {
                    result.Add(new
                    {
                        DoctorProfileId = doctor.Id,
                        DoctorName = docUser?.FullName ?? "Unknown",
                        Department = department?.Name ?? "",
                        DepartmentId = doctor.DepartmentId,
                        Specialization = doctor.Specialization,
                        IsAvailableToday = false,
                        Slots = new List<object>()
                    });
                    continue;
                }

                // Generate all slots and overlay appointment data
                var slots = new List<object>();
                var slotDuration = TimeSpan.FromMinutes(doctor.SlotDurationMinutes);

                void AddSlots(TimeSpan start, TimeSpan end, string session)
                {
                    var current = start;
                    while (current < end)
                    {
                        var appt = appointments.FirstOrDefault(a =>
                            a.DoctorProfileId == doctor.Id && a.TimeSlot == current
                            && a.Status != "Cancelled");

                        string status = "Available";
                        int? apptId = null;
                        string? patientName = null;
                        string? tokenNumber = null;
                        string? visitType = null;

                        if (appt != null)
                        {
                            apptId = appt.Id;
                            tokenNumber = appt.TokenNumber;
                            visitType = appt.VisitType;
                            // Get patient name
                            var patient = _context.Users.Find(appt.PatientUserId);
                            patientName = patient?.FullName;

                            status = appt.Status switch
                            {
                                "Scheduled" => appt.VisitType is "Walk-in" or "Emergency" ? "WalkIn" : "Booked",
                                "CheckedIn" => "CheckedIn",
                                "WithDoctor" => "WithDoctor",
                                "Completed" => "Completed",
                                "NoShow" => "NoShow",
                                "Cancelled" => "Available",
                                _ => "Booked"
                            };
                        }

                        slots.Add(new
                        {
                            Time = current.ToString(@"hh\:mm"),
                            DisplayTime = DateTime.Today.Add(current).ToString("hh:mm tt"),
                            Status = status,
                            AppointmentId = apptId,
                            PatientName = patientName,
                            TokenNumber = tokenNumber,
                            VisitType = visitType,
                            Session = session
                        });

                        current = current.Add(slotDuration);
                    }
                }

                AddSlots(doctor.MorningStart, doctor.MorningEnd, "Morning");
                if (doctor.HasEveningShift && doctor.EveningStart.HasValue && doctor.EveningEnd.HasValue)
                    AddSlots(doctor.EveningStart.Value, doctor.EveningEnd.Value, "Evening");

                result.Add(new
                {
                    DoctorProfileId = doctor.Id,
                    DoctorName = docUser?.FullName ?? "Unknown",
                    Department = department?.Name ?? "",
                    DepartmentId = doctor.DepartmentId,
                    Specialization = doctor.Specialization,
                    IsAvailableToday = true,
                    Slots = slots
                });
            }

            return Ok(new { success = true, data = result });
        }

        // GET api/appointments/queue?doctorProfileId=1&date=2026-03-10
        [HttpGet("queue")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,Receptionist,Doctor,Nurse")]
        public async Task<IActionResult> GetQueue(
            [FromQuery] int doctorProfileId,
            [FromQuery] DateTime date)
        {
            var dateOnly = date.Date;
            var doctor = await _context.DoctorProfiles.FindAsync(doctorProfileId);
            if (doctor == null) return NotFound(new { success = false, message = "Doctor not found" });

            var queue = await _context.Appointments
                .Where(a => a.DoctorProfileId == doctorProfileId
                    && a.AppointmentDate.Date == dateOnly
                    && a.Status != "Cancelled" && a.Status != "Completed" && a.Status != "NoShow")
                .OrderBy(a => a.QueuePosition > 0 ? a.QueuePosition : 9999)
                .ThenBy(a => a.TimeSlot)
                .ToListAsync();

            var result = new List<object>();
            int position = 1;
            foreach (var appt in queue)
            {
                var patient = await _context.Users.FindAsync(appt.PatientUserId);
                var estimatedWait = (position - 1) * doctor.SlotDurationMinutes;
                result.Add(new
                {
                    Position = position,
                    appt.TokenNumber,
                    PatientName = patient?.FullName ?? "Unknown",
                    PatientPhone = patient?.PhoneNumber ?? "",
                    appt.Status,
                    CheckedInAt = appt.CheckedInAt.HasValue ? appt.CheckedInAt.Value.ToString("HH:mm") : null,
                    appt.TimeSlot,
                    EstimatedWaitMinutes = estimatedWait
                });
                position++;
            }

            var currentToken = queue.FirstOrDefault(a => a.Status == "WithDoctor")?.TokenNumber;
            return Ok(new { success = true, data = result, currentToken });
        }

        // POST api/appointments — Create appointment (existing patient OR walk-in)
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,Receptionist,Doctor,Patient")]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
        {
            try
            {
                int patientUserId;

                if (dto.IsWalkIn || dto.PatientUserId == null)
                {
                    // Walk-in: create or find a guest patient account
                    if (string.IsNullOrWhiteSpace(dto.PatientName) || string.IsNullOrWhiteSpace(dto.PatientPhone))
                        return BadRequest(new { success = false, message = "Patient name and phone are required for walk-in." });

                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.PhoneNumber == dto.PatientPhone);

                    if (existingUser != null)
                    {
                        patientUserId = existingUser.Id;
                    }
                    else
                    {
                        // Create a minimal guest account — no email required for walk-in
                        var tempPassword = $"Med@{new Random().Next(1000, 9999)}";
                        var guestUser = new MediCore.API.Modules.Auth.Models.User
                        {
                            FullName = dto.PatientName,
                            Email = $"walkin_{dto.PatientPhone}@medicore.guest",
                            PhoneNumber = dto.PatientPhone,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UserRoles = new List<MediCore.API.Modules.Auth.Models.UserRole>
                            {
                                new MediCore.API.Modules.Auth.Models.UserRole { RoleId = 10 }
                            }
                        };
                        _context.Users.Add(guestUser);
                        await _context.SaveChangesAsync();
                        patientUserId = guestUser.Id;
                    }
                }
                else
                {
                    patientUserId = dto.PatientUserId.Value;
                    var patient = await _context.Users.FindAsync(patientUserId);
                    if (patient == null)
                        return NotFound(new { success = false, message = "Patient not found." });
                }

                var doctorProfile = await _context.DoctorProfiles.FindAsync(dto.DoctorProfileId);
                if (doctorProfile == null)
                    return BadRequest(new { success = false, message = "Doctor profile not found." });

                // Generate token number
                var tokenCount = await _context.Appointments
                    .CountAsync(a => a.AppointmentDate.Date == dto.AppointmentDate.Date
                        && a.DoctorProfileId == dto.DoctorProfileId);
                var tokenNumber = $"OPD{dto.AppointmentDate:yyyyMMdd}{(tokenCount + 1):D3}";

                int deptId = dto.DepartmentId ?? doctorProfile.DepartmentId;

                var appointment = new MediCore.API.Modules.OPD.Models.Appointment
                {
                    TokenNumber = tokenNumber,
                    PatientUserId = patientUserId,
                    DoctorProfileId = dto.DoctorProfileId,
                    DepartmentId = deptId,
                    AppointmentDate = dto.AppointmentDate,
                    TimeSlot = TimeSpan.TryParse(dto.TimeSlot, out var ts) ? ts : DateTime.Parse(dto.TimeSlot).TimeOfDay,
                    VisitType = dto.VisitType ?? (dto.IsWalkIn ? "Walk-in" : "Consultation"),
                    Symptoms = dto.Symptoms ?? string.Empty,
                    IsFirstVisit = true,
                    Status = "Scheduled",
                    ConsultationFee = doctorProfile.ConsultationFee,
                    PaymentStatus = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Appointment created successfully",
                    data = new
                    {
                        appointment.Id,
                        appointment.TokenNumber,
                        appointment.AppointmentDate,
                        appointment.Status,
                        appointment.ConsultationFee
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        // GET api/appointments/slots
        // Get available slots for a doctor on a date
        [HttpGet("slots")]
        public async Task<IActionResult> GetAvailableSlots(
            [FromQuery] int doctorProfileId,
            [FromQuery] DateTime date)
        {
            var doctor = await _context.DoctorProfiles.FindAsync(doctorProfileId);
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor not found" });

            // Check if doctor works on this day
            var dayName = date.DayOfWeek.ToString()[..3];
            if (!doctor.AvailableDays.Contains(dayName))
                return Ok(new { success = true, data = new List<object>(), message = "Doctor not available on this day" });

            // Check for Leaves
            var onLeave = await _context.DoctorLeaves
                .AnyAsync(l => l.DoctorProfileId == doctorProfileId 
                    && l.Status == "Approved"
                    && date.Date >= l.StartDate.Date 
                    && date.Date <= l.EndDate.Date);

            if (onLeave)
                return Ok(new { success = true, data = new List<object>(), message = "Doctor is on leave on this day" });

            // Get already booked slots
            var bookedSlots = await _context.Appointments
                .Where(a => a.DoctorProfileId == doctorProfileId
                    && a.AppointmentDate.Date == date.Date
                    && a.Status != "Cancelled")
                .Select(a => a.TimeSlot)
                .ToListAsync();

            // Generate all slots
            var slots = new List<object>();
            var slotDuration = TimeSpan.FromMinutes(doctor.SlotDurationMinutes);

            // Morning slots
            var current = doctor.MorningStart;
            while (current < doctor.MorningEnd)
            {
                slots.Add(new
                {
                    time = current.ToString(@"hh\:mm"),
                    displayTime = DateTime.Today.Add(current).ToString("hh:mm tt"),
                    isBooked = bookedSlots.Contains(current),
                    session = "Morning"
                });
                current = current.Add(slotDuration);
            }

            // Evening slots
            if (doctor.HasEveningShift && doctor.EveningStart.HasValue && doctor.EveningEnd.HasValue)
            {
                current = doctor.EveningStart.Value;
                while (current < doctor.EveningEnd.Value)
                {
                    slots.Add(new
                    {
                        time = current.ToString(@"hh\:mm"),
                        displayTime = DateTime.Today.Add(current).ToString("hh:mm tt"),
                        isBooked = bookedSlots.Contains(current),
                        session = "Evening"
                    });
                    current = current.Add(slotDuration);
                }
            }

            return Ok(new { success = true, data = slots });
        }

        // POST api/appointments/{id}/send-reminder
        [HttpPost("{id}/send-reminder")]
        [Authorize(Roles = "SuperAdmin,HospitalAdmin,Receptionist,Patient")]
        public async Task<IActionResult> SendReminder(int id, [FromServices] IEmailService emailService)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound(new { success = false, message = "Appointment not found" });

            var patient = await _context.Users.FindAsync(appt.PatientUserId);
            var doctor = await _context.DoctorProfiles.FindAsync(appt.DoctorProfileId);
            var docUser = doctor != null ? await _context.Users.FindAsync(doctor.UserId) : null;
            var dept = await _context.Departments.FindAsync(appt.DepartmentId);

            if (patient == null || string.IsNullOrEmpty(patient.Email))
                return BadRequest(new { success = false, message = "Patient email not found" });

            await emailService.SendAppointmentReminderAsync(
                patient.Email,
                patient.FullName,
                docUser?.FullName ?? "Doctor",
                dept?.Name ?? "OPD",
                appt.AppointmentDate.ToString("dddd, dd MMMM yyyy"),
                DateTime.Today.Add(appt.TimeSlot).ToString("hh:mm tt"),
                appt.TokenNumber,
                appt.ConsultationFee
            );

            return Ok(new { success = true, message = "Reminder sent to " + patient.Email });
        }

        // GET api/appointments/{id}/qr-data
        [HttpGet("{id}/qr-data")]
        [Authorize]
        public async Task<IActionResult> GetQrData(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound(new { success = false, message = "Appointment not found" });

            var patient = await _context.Users.FindAsync(appt.PatientUserId);
            var doctor = await _context.DoctorProfiles.FindAsync(appt.DoctorProfileId);
            var docUser = doctor != null ? await _context.Users.FindAsync(doctor.UserId) : null;
            var dept = await _context.Departments.FindAsync(appt.DepartmentId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    appt.Id,
                    appt.TokenNumber,
                    PatientName = patient?.FullName ?? "",
                    DoctorName = docUser?.FullName ?? "",
                    Department = dept?.Name ?? "",
                    Date = appt.AppointmentDate.ToString("dd-MMM-yyyy"),
                    Time = DateTime.Today.Add(appt.TimeSlot).ToString("hh:mm tt"),
                    appt.ConsultationFee,
                    appt.Status
                }
            });
        }
    }

    public class UpdateAppointmentStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? DoctorNotes { get; set; }
    }

    public class UpdatePaymentDto
    {
        public string PaymentMode { get; set; } = string.Empty;
        public string? RazorpayPaymentId { get; set; }
    }

    public class CreateAppointmentDto
    {
        // For existing patient booking
        public int? PatientUserId { get; set; }

        // For walk-in booking
        public bool IsWalkIn { get; set; } = false;
        public string? PatientName { get; set; }
        public string? PatientPhone { get; set; }
        public int? PatientAge { get; set; }
        public string? PatientGender { get; set; }

        // Appointment details
        public int DoctorProfileId { get; set; }
        public int? DepartmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string TimeSlot { get; set; } = string.Empty;
        public string? Symptoms { get; set; }
        public string? VisitType { get; set; }
    }
}



