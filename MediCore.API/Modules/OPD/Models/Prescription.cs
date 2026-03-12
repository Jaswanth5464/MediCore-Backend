namespace MediCore.API.Modules.OPD.Models
{
    public class Prescription
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public int PatientUserId { get; set; }
        public int DoctorProfileId { get; set; }

        public string Diagnosis { get; set; } = string.Empty;
        public string MedicinesJson { get; set; } = "[]"; // JSON array of medicines
        public string? Advice { get; set; }
        public string? DietPlan { get; set; } // New field for diet plan
        public bool IsDispensed { get; set; } = false; // For pharmacy tracking
        public DateTime? FollowUpDate { get; set; }
        public string? Notes { get; set; }
        public string? PdfUrl { get; set; }

        public DateTime? DispensedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Appointment? Appointment { get; set; }
    }
}
