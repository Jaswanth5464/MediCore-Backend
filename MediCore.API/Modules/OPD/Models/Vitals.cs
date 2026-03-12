namespace MediCore.API.Modules.OPD.Models
{
    public class Vitals
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public int PatientUserId { get; set; }

        // Vitals
        public int? BloodPressureSystolic { get; set; }
        public int? BloodPressureDiastolic { get; set; }
        public int? Pulse { get; set; }             // bpm
        public decimal? Temperature { get; set; }    // Celsius
        public int? SpO2 { get; set; }               // %
        public decimal? Weight { get; set; }         // kg
        public decimal? Height { get; set; }         // cm

        public int RecordedById { get; set; } // Nurse or Receptionist User ID
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
