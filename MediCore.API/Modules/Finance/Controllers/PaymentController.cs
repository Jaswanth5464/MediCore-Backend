using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediCore.API.Infrastructure.Database.Context;
using Microsoft.EntityFrameworkCore;

namespace MediCore.API.Modules.Finance.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly MediCoreDbContext _context;

        public PaymentController(MediCoreDbContext context)
        {
            _context = context;
        }

        // POST api/payment/create-order
        [HttpPost("create-order")]
        [Authorize(Roles = "Patient,SuperAdmin")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var bill = await _context.Bills.FindAsync(request.BillId);
            if (bill == null)
                return NotFound(new { success = false, message = "Bill not found" });

            if (bill.Status == "Paid")
                return BadRequest(new { success = false, message = "Bill is already paid" });

            // Generate a Mock Razorpay Order ID
            var orderId = $"order_Rmock_{Guid.NewGuid().ToString("N").Substring(0, 10)}";
            
            return Ok(new 
            { 
                success = true, 
                orderId = orderId, 
                amount = bill.TotalAmount * 100 // Razorpay expects paise/cents
            });
        }

        // POST api/payment/verify
        [HttpPost("verify")]
        [Authorize(Roles = "Patient,SuperAdmin")]
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest request)
        {
            var bill = await _context.Bills.FindAsync(request.BillId);
            if (bill == null)
                return NotFound(new { success = false, message = "Bill not found" });

            // In a real app, you would verify the signature using RazorpayClient
            
            // Mark bill as Paid
            bill.Status = "Paid";
            bill.PaymentMode = "Online Payment";
            bill.RazorpayOrderId = request.RazorpayOrderId;
            bill.RazorpayPaymentId = request.RazorpayPaymentId;
            bill.PaidAt = DateTime.UtcNow;
            bill.UpdatedAt = DateTime.UtcNow;

            // Mark associated appointment as Paid too
            var appt = await _context.Appointments.FindAsync(bill.AppointmentId);
            if (appt != null)
            {
                appt.PaymentStatus = "Paid";
                appt.PaymentMode = "Online Payment";
                appt.RazorpayPaymentId = request.RazorpayPaymentId;
                appt.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Payment verified successfully" });
        }
    }

    public class CreateOrderRequest
    {
        public int BillId { get; set; }
    }

    public class VerifyPaymentRequest
    {
        public int BillId { get; set; }
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string RazorpaySignature { get; set; } = string.Empty;
    }
}
