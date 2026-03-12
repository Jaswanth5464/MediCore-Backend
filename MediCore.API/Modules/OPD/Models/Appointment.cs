using MediCore.API.Modules.Patient.Models;
using MediCore.API.Modules.Auth.Models;

namespace MediCore.API.Modules.OPD.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public string TokenNumber { get; set; } = string.Empty;

        // Who
        public int PatientUserId { get; set; }
        public int DoctorProfileId { get; set; }
        public int DepartmentId { get; set; }

        // When
        public DateTime AppointmentDate { get; set; }
        public TimeSpan TimeSlot { get; set; }

        // Details
        public string VisitType { get; set; } = "Consultation";
        public string Symptoms { get; set; } = string.Empty;
        public bool IsFirstVisit { get; set; } = true;

        // Status
        public string Status { get; set; } = "Scheduled";
        // Scheduled, Confirmed, CheckedIn, WithDoctor, Completed, Cancelled, NoShow

        // Payment
        public decimal ConsultationFee { get; set; }
        public string PaymentStatus { get; set; } = "Pending";
        // Pending, Paid, Waived
        public string? PaymentMode { get; set; }
        // Cash, Card, UPI, Online, Insurance
        public string? RazorpayOrderId { get; set; }
        public string? RazorpayPaymentId { get; set; }

        // From request
        public int? AppointmentRequestId { get; set; }

        // Notes
        public string? DoctorNotes { get; set; }
        public string? ReceptionistNotes { get; set; }

        // Queue / timeline tracking
        public DateTime? CheckedInAt { get; set; }
        public DateTime? ConsultationStartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int QueuePosition { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public Department Department { get; set; } = null!;
        public User PatientUser { get; set; } = null!;
        public MediCore.API.Modules.Doctor.Models.DoctorProfile DoctorProfile { get; set; } = null!;
    }
}