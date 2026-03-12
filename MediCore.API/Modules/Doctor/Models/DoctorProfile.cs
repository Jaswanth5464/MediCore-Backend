using MediCore.API.Modules.Auth.Models;
using MediCore.API.Modules.Patient.Models;

namespace MediCore.API.Modules.Doctor.Models
{
    public class DoctorProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int DepartmentId { get; set; }
        public string Specialization { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public decimal ConsultationFee { get; set; }
        public string AvailableDays { get; set; } = string.Empty;

        public TimeSpan MorningStart { get; set; }
        public TimeSpan MorningEnd { get; set; }

        public bool HasEveningShift { get; set; }

        public TimeSpan? EveningStart { get; set; }
        public TimeSpan? EveningEnd { get; set; }

        public int SlotDurationMinutes { get; set; } = 15;
        public int MaxPatientsPerDay { get; set; } = 30;

        public string Bio { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public Department Department { get; set; } = null!;
    }
}