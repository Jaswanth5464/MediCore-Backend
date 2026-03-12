using MediCore.API.Modules.OPD.Models;

namespace MediCore.API.Modules.Laboratory.Models
{
    public class LabOrder
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public int PatientUserId { get; set; }
        public int DoctorProfileId { get; set; }

        public string TestType { get; set; } = string.Empty;
        public string? Notes { get; set; } // Notes from doctor
        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed
        public string? ResultNotes { get; set; } // Notes from lab technician
        public string? ReportUrl { get; set; }
        public bool CriticalAlert { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public Appointment? Appointment { get; set; }
    }
}
