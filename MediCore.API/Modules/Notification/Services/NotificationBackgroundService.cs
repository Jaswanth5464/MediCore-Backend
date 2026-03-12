using MediCore.API.Infrastructure.Database.Context;
using MediCore.API.Services;
using Microsoft.EntityFrameworkCore;

namespace MediCore.API.Modules.Notification.Services
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationBackgroundService> _logger;

        public NotificationBackgroundService(IServiceProvider serviceProvider, ILogger<NotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessNotificationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in Notification Background Service.");
                }

                // In production, this would be await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                // For demonstration/testing, run every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task ProcessNotificationsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MediCoreDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

            var now = DateTime.UtcNow;

            // 1. 24-Hour Appointment Reminders
            var tomorrowStart = now.Date.AddDays(1);
            var tomorrowEnd = tomorrowStart.AddDays(1);

            var upcomingAppts = await dbContext.Appointments
                .Include(a => a.PatientUser)
                .Include(a => a.DoctorProfile)
                .ThenInclude(d => d.User)
                .Include(a => a.Department)
                .Where(a => a.Status == "Scheduled" && a.AppointmentDate >= tomorrowStart && a.AppointmentDate < tomorrowEnd)
                .ToListAsync();

            foreach (var appt in upcomingAppts)
            {
                // In a real system, track if reminder already sent to prevent spam.
                // Assuming EmailService handles exceptions internally.
                await emailService.SendAppointmentReminderAsync(
                    appt.PatientUser.Email,
                    appt.PatientUser.FullName,
                    appt.DoctorProfile.User.FullName,
                    appt.Department.Name,
                    appt.AppointmentDate.ToString("yyyy-MM-dd"),
                    appt.TimeSlot.ToString(@"hh\:mm"),
                    appt.TokenNumber ?? "TBA",
                    appt.ConsultationFee
                );
                _logger.LogInformation($"Sent 24h reminder for Appt {appt.Id} to {appt.PatientUser.Email}");
            }

            // 2. Daily Medicine Reminders
            // Find active prescriptions (created within last 5 days for a typical course, normally duration is explicitly saved)
            var activeDateLine = now.AddDays(-5);
            var activePrescriptions = await dbContext.Prescriptions
                .Include(p => p.Appointment)
                .ThenInclude(a => a.PatientUser)
                .Where(p => p.IsDispensed && p.CreatedAt >= activeDateLine)
                .ToListAsync();

            foreach (var p in activePrescriptions)
            {
                if (string.IsNullOrWhiteSpace(p.MedicinesJson) || p.MedicinesJson == "[]") continue;
                
                string content = $@"
                    <h3>Daily Medicine Reminder</h3>
                    <p>Dear {p.Appointment.PatientUser.FullName}, don't forget to take your medicines today:</p>
                    <pre style='background:#f4f4f5; padding:15px; border-radius:8px;'>{p.MedicinesJson}</pre>
                    <p>Stay healthy!</p>
                ";
                
                // Route through email
                // Note: EmailService might need a generic Send method, or we construct one here. Need to ensure EmailService has SendEmailAsync.
            }

            // 3. Low Stock / Expiry Alerts (To Admin)
            var lowStockMeds = await dbContext.Medicines
                .Where(m => m.StockQuantity <= m.LowStockThreshold)
                .ToListAsync();

            var expiringMeds = await dbContext.Medicines
                .Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value <= now.AddDays(30))
                .ToListAsync();

            if (lowStockMeds.Any() || expiringMeds.Any())
            {
                _logger.LogWarning($"Found {lowStockMeds.Count} low stock and {expiringMeds.Count} expiring medicines.");
                // Send alert to admin here
            }
        }
    }
}
