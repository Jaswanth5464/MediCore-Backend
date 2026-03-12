namespace MediCore.API.Modules.OPD.Models
{
    public class AppointmentRequest
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;

        // Patient Details
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;

        // Visit Details
        public int? PreferredDepartmentId { get; set; }
        public string Symptoms { get; set; } = string.Empty;
        public DateTime? PreferredDate { get; set; }
        public string VisitType { get; set; } = "Consultation";
        public bool IsFirstVisit { get; set; } = true;

        // Status
        public string Status { get; set; } = "Pending";
        // Pending, Contacted, Confirmed, Rejected, Converted

        public string? RejectionReason { get; set; }
        public string? ReceptionistNotes { get; set; }
        public int? HandledByUserId { get; set; }
        public DateTime? HandledAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}