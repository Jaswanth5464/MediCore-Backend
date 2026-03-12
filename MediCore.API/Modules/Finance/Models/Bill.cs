using MediCore.API.Modules.OPD.Models;

namespace MediCore.API.Modules.Finance.Models
{
    public class Bill
    {
        public int Id { get; set; }
        public string BillNumber { get; set; } = string.Empty;

        // Relationships
        public int AppointmentId { get; set; }
        public int PatientUserId { get; set; }
        public int DoctorProfileId { get; set; }
 
        public Appointment Appointment { get; set; } = null!;

        // Items - stored as JSON string
        public string Items { get; set; } = "[]";

        // Financials
        public decimal SubTotal { get; set; }
        public decimal GSTPercent { get; set; } = 0;
        public decimal GSTAmount { get; set; } = 0;
        public decimal Discount { get; set; } = 0;
        public decimal InsuranceDeduction { get; set; } = 0;
        public decimal TotalAmount { get; set; }

        // Payment
        public string Status { get; set; } = "Unpaid"; // Unpaid, Paid, Waived, Cancelled
        public string? PaymentMode { get; set; } // Cash, Card, UPI, Online, Insurance
        public string? RazorpayOrderId { get; set; }
        public string? RazorpayPaymentId { get; set; }
        public DateTime? PaidAt { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
