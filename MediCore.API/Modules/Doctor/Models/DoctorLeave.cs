using MediCore.API.Modules.Doctor.Models;

namespace MediCore.API.Modules.Doctor.Models
{
    public class DoctorLeave
    {
        public int Id { get; set; }
        public int DoctorProfileId { get; set; }
        public DoctorProfile DoctorProfile { get; set; } = null!;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
