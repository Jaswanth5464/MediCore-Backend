using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace MediCore.API.Services
{
    public interface IEmailService
    {
        Task SendAppointmentRequestConfirmationAsync(
            string toEmail, string patientName, string referenceNumber);
        Task SendAppointmentConfirmedAsync(
            string toEmail, string patientName,
            string doctorName, string date,
            string time, string tokenNumber,
            decimal fee, string loginEmail, string tempPassword);
        Task SendOtpEmailAsync(string toEmail, string otpCode);
        Task SendInvoiceAsync(string toEmail, string patientName, string billNumber, decimal amount, string date, string status);
        Task SendLabReportAsync(string toEmail, string patientName, string testType, string results, string date, string reportUrl);
        Task SendDailyDigestAsync(string adminEmail, string adminName, int totalPatients, decimal totalRevenue, int pendingAppts, int tomorrowCount, string dietPlanHtml = "");
        Task SendAppointmentReminderAsync(string toEmail, string patientName, string doctorName, string department, string date, string time, string tokenNumber, decimal fee);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        private readonly string _hospitalSignature = @"
            <div style='border-top: 1px solid #e2e8f0; padding-top: 24px; margin-top: 40px; text-align:left;'>
                <table style='width:100%; border-collapse:collapse;'>
                    <tr>
                        <td style='width:56px; vertical-align:top; padding-right:16px;'>
                            <div style='width:56px; height:56px; background:linear-gradient(135deg, #0f172a, #1e293b); color:white; border-radius:14px; text-align:center; line-height:56px; font-weight:bold; font-size:22px; box-shadow:0 4px 12px rgba(15,23,42,0.15); font-family:sans-serif;'>MC</div>
                        </td>
                        <td style='vertical-align:top; font-family:sans-serif;'>
                            <div style='font-weight:700; color:#0f172a; font-size:16px; margin-bottom:2px;'>MediCore Administration</div>
                            <div style='color:#64748b; font-size:14px; margin-bottom:6px;'>Department of Patient Services</div>
                            <div style='color:#3b82f6; font-size:13px; font-weight:600;'>🏥 MediCore Super Specialty Hospital</div>
                            <div style='color:#94a3b8; font-size:12px; margin-top:2px;'>123 Healthcare Avenue, Hyderabad • 📞 +91 98765 43210</div>
                        </td>
                    </tr>
                </table>
            </div>";

        public async Task SendAppointmentRequestConfirmationAsync(
            string toEmail, string patientName, string referenceNumber)
        {
            var subject = $"Appointment Request Received — {referenceNumber}";

            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<style>
  body {{ margin: 0; padding: 0; background-color: #f8fafc; font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased; }}
  .wrapper {{ width: 100%; padding: 40px 0; background-color: #f8fafc; }}
  .main {{ margin: 0 auto; width: 100%; max-width: 600px; background: #ffffff; border-radius: 16px; box-shadow: 0 10px 40px rgba(0,0,0,0.06); overflow: hidden; border: 1px solid #f1f5f9; }}
  .header {{ background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%); padding: 48px 40px; text-align: center; border-bottom: 5px solid #3b82f6; }}
  .logo {{ display: inline-block; width: 64px; height: 64px; background: linear-gradient(135deg, #3b82f6, #2563eb); border-radius: 16px; line-height: 64px; color: white; font-size: 32px; font-weight: 800; margin-bottom: 24px; box-shadow: 0 8px 24px rgba(59,130,246,0.3); }}
  .header h1 {{ margin: 0; color: #ffffff; font-size: 28px; font-weight: 700; letter-spacing: -0.5px; }}
  .header p {{ margin: 8px 0 0; color: #94a3b8; font-size: 15px; font-weight: 400; }}
  .content {{ padding: 48px 40px; }}
  .greeting {{ color: #0f172a; font-size: 20px; font-weight: 600; margin-bottom: 20px; }}
  .message {{ color: #475569; font-size: 16px; line-height: 1.7; margin-bottom: 32px; }}
  .callout {{ background: linear-gradient(135deg, #f0fdf4 0%, #dcfce7 100%); border-radius: 12px; padding: 24px; text-align: center; margin-bottom: 32px; border: 1px solid #bbf7d0; }}
  .callout-label {{ color: #166534; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: 1.5px; margin-bottom: 8px; }}
  .callout-value {{ color: #14532d; font-size: 32px; font-weight: 800; letter-spacing: 2px; }}
  .footer {{ background: #f8fafc; padding: 32px 40px; text-align: center; border-top: 1px solid #f1f5f9; }}
  .footer p {{ margin: 0; color: #94a3b8; font-size: 13px; line-height: 1.5; }}
</style>
</head>
<body>
<div class='wrapper'>
  <div class='main'>
    <div class='header'>
      <div class='logo'>MC</div>
      <h1>Request Received</h1>
      <p>Your appointment request has been logged</p>
    </div>
    <div class='content'>
      <div class='greeting'>Hello {patientName},</div>
      <div class='message'>
        Thank you for reaching out to MediCore Hospitals. We are writing to confirm that our systems have successfully captured your appointment request. 
        <br><br>
        Our medical assignment team is currently reviewing your chosen department and symptoms to match you with the best available specialist.
      </div>
      <div class='callout'>
        <div class='callout-label'>Your Reference Code</div>
        <div class='callout-value'>{referenceNumber}</div>
      </div>
      <div class='message'>
        <strong>What's Next?</strong><br>
        Please expect a confirmation email and a call from our administration desk within the next <strong>2 hours</strong> to finalize your booking slot.
      </div>
      {_hospitalSignature}
    </div>
    <div class='footer'>
      <p>This is a system-generated message. Please do not reply directly to this email.</p>
      <p style='margin-top: 12px;'>© 2026 MediCore Hospitals. All rights reserved.</p>
    </div>
  </div>
</div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendAppointmentConfirmedAsync(
            string toEmail, string patientName,
            string doctorName, string date,
            string time, string tokenNumber,
            decimal fee, string loginEmail, string tempPassword)
        {
            var subject = $"Action Required: Appointment Confirmed with {doctorName}";

            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<style>
  body {{ margin: 0; padding: 0; background-color: #f8fafc; font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased; }}
  .wrapper {{ width: 100%; padding: 40px 0; background-color: #f8fafc; }}
  .main {{ margin: 0 auto; width: 100%; max-width: 600px; background: #ffffff; border-radius: 16px; box-shadow: 0 10px 40px rgba(0,0,0,0.06); overflow: hidden; border: 1px solid #f1f5f9; }}
  .header {{ background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 48px 40px; text-align: center; }}
  .logo {{ display: inline-block; width: 64px; height: 64px; background: #ffffff; border-radius: 50%; line-height: 64px; font-size: 32px; margin-bottom: 24px; box-shadow: 0 8px 24px rgba(0,0,0,0.1); }}
  .header h1 {{ margin: 0; color: #ffffff; font-size: 28px; font-weight: 700; letter-spacing: -0.5px; }}
  .header p {{ margin: 8px 0 0; color: #d1fae5; font-size: 15px; font-weight: 500; text-transform: uppercase; letter-spacing: 1px; }}
  .content {{ padding: 48px 40px; }}
  .greeting {{ color: #0f172a; font-size: 20px; font-weight: 600; margin-bottom: 24px; }}
  .message {{ color: #475569; font-size: 16px; line-height: 1.7; margin-bottom: 32px; }}
  .data-card {{ background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 24px; margin-bottom: 32px; }}
  .data-row {{ display: table; width: 100%; margin-bottom: 16px; padding-bottom: 16px; border-bottom: 1px solid #f1f5f9; }}
  .data-row:last-child {{ margin-bottom: 0; padding-bottom: 0; border-bottom: none; }}
  .data-label {{ display: table-cell; color: #64748b; font-size: 14px; font-weight: 500; width: 40%; }}
  .data-value {{ display: table-cell; color: #0f172a; font-size: 15px; font-weight: 700; text-align: right; width: 60%; }}
  .token-area {{ background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%); border-radius: 12px; padding: 32px; text-align: center; margin-bottom: 32px; box-shadow: 0 10px 25px rgba(15,23,42,0.2); }}
  .token-label {{ color: #94a3b8; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: 2px; margin-bottom: 12px; }}
  .token-value {{ color: #ffffff; font-size: 48px; font-weight: 800; letter-spacing: 4px; line-height: 1; }}
  .creds-box {{ background: #fffbeb; border: 1px solid #fde68a; border-radius: 12px; padding: 24px; margin-bottom: 32px; }}
  .creds-title {{ color: #b45309; font-size: 15px; font-weight: 700; margin: 0 0 16px 0; }}
  .creds-p {{ color: #78350f; font-size: 14px; margin: 0 0 8px 0; }}
  .footer {{ background: #f8fafc; padding: 32px 40px; text-align: center; border-top: 1px solid #f1f5f9; }}
  .footer p {{ margin: 0; color: #94a3b8; font-size: 13px; line-height: 1.5; }}
</style>
</head>
<body>
<div class='wrapper'>
  <div class='main'>
    <div class='header'>
      <div class='logo'>✅</div>
      <h1>Appointment Confirmed</h1>
      <p>You are successfully scheduled</p>
    </div>
    <div class='content'>
      <div class='greeting'>Hello {patientName},</div>
      <div class='message'>
        Great news! Your medical appointment at MediCore Hospitals has been fully approved and confirmed. Please review your scheduled details below.
      </div>

      <div class='data-card'>
        <div class='data-row'>
          <div class='data-label'>Attending Doctor</div>
          <div class='data-value'>{doctorName}</div>
        </div>
        <div class='data-row'>
          <div class='data-label'>Scheduled Date</div>
          <div class='data-value'>{date}</div>
        </div>
        <div class='data-row'>
          <div class='data-label'>Arrival Time</div>
          <div class='data-value'>{time}</div>
        </div>
        <div class='data-row'>
          <div class='data-label'>Est. Consultation Fee</div>
          <div class='data-value'>₹{fee}</div>
        </div>
      </div>

      <div class='token-area'>
        <div class='token-label'>Queue Token Number</div>
        <div class='token-value'>{tokenNumber}</div>
      </div>

      {(string.IsNullOrEmpty(tempPassword) ? "" : $@"
      <div class='creds-box'>
        <h4 class='creds-title'>🔐 Patient Portal Account Created</h4>
        <p class='creds-p'>We've generated a secure portal account to track your test results and prescriptions.</p>
        <p class='creds-p'><strong>Login:</strong> {loginEmail}</p>
        <p class='creds-p'><strong>Password:</strong> {tempPassword}</p>
        <p style='color:#b45309; font-size:12px; margin:16px 0 0 0; font-style:italic;'>We strongly recommend logging in and changing your password before your visit.</p>
      </div>")}

      <div class='message' style='background:#f1f5f9; padding:16px; border-radius:8px; font-size:14px;'>
        <strong>Hospital Entry:</strong> Present this email or state your token number (<strong>{tokenNumber}</strong>) at the front desk. Please enter the premises 15 minutes before your time slot.
      </div>

      {_hospitalSignature}
    </div>
    <div class='footer'>
      <p>This is a system-generated message. Please do not reply directly to this email.</p>
      <p style='margin-top: 12px;'>© 2026 MediCore Hospitals. All rights reserved.</p>
    </div>
  </div>
</div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode)
        {
            var subject = "Your MediCore Security Shield Code";

            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<style>
  body {{ margin: 0; padding: 0; background-color: #f8fafc; font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased; }}
  .wrapper {{ width: 100%; padding: 40px 0; background-color: #f8fafc; }}
  .main {{ margin: 0 auto; width: 100%; max-width: 500px; background: #ffffff; border-radius: 16px; box-shadow: 0 10px 40px rgba(0,0,0,0.06); overflow: hidden; border: 1px solid #f1f5f9; }}
  .header {{ background: linear-gradient(135deg, #1e293b 0%, #334155 100%); padding: 40px; text-align: center; border-bottom: 4px solid #f59e0b; }}
  .header h1 {{ margin: 0; color: #ffffff; font-size: 24px; font-weight: 700; letter-spacing: -0.5px; }}
  .content {{ padding: 32px 20px; text-align: center; }}
  .message {{ color: #475569; font-size: 15px; line-height: 1.6; margin-bottom: 24px; padding: 0 16px; }}
  .otp-display {{ background: #f8fafc; border: 2px dashed #94a3b8; border-radius: 12px; padding: 20px 16px; margin: 0 auto 32px auto; display: inline-block; min-width: 220px; box-shadow: inset 0 2px 4px rgba(0,0,0,0.02); }}
  .otp-code {{ color: #0f172a; font-size: 40px; font-weight: 800; letter-spacing: 6px; font-family: 'Courier New', monospace; line-height: 1; margin: 0; text-align: center; }}
  .warning {{ color: #ef4444; font-size: 13px; font-weight: 600; padding: 12px 20px; background: #fef2f2; border-radius: 8px; display: inline-block; border: 1px solid #fee2e2; }}
  .footer {{ background: #f8fafc; padding: 24px 40px; text-align: center; border-top: 1px solid #f1f5f9; }}
  .footer p {{ margin: 0; color: #94a3b8; font-size: 12px; line-height: 1.5; }}
</style>
</head>
<body>
<div class='wrapper'>
  <div class='main'>
    <div class='header'>
      <h1>Security Verification</h1>
    </div>
    <div class='content'>
      <div class='message'>
        You are attempting to request an appointment on the MediCore Patient Portal. To proceed, please use the 6-digit verification code below.
      </div>
      <div class='otp-display'>
        <div class='otp-code'>{otpCode}</div>
      </div>
      <div>
        <div class='warning'>
          ⚠️ This code expires in 5 minutes. Never share this with anyone.
        </div>
      </div>
      {_hospitalSignature}
    </div>
    <div class='footer'>
      <p>MediCore Automated Security System</p>
      <p style='margin-top: 8px;'>If you did not request this code, please ignore this email.</p>
    </div>
  </div>
</div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendInvoiceAsync(string toEmail, string patientName, string billNumber, decimal amount, string date, string status)
        {
            var subject = $"Invoice for your visit — {billNumber}";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
<style>
  body {{ font-family: sans-serif; background: #f4f7f6; padding: 20px; }}
  .card {{ background: white; max-width: 500px; margin: auto; padding: 30px; border-radius: 12px; border-top: 5px solid #4f46e5; box-shadow: 0 10px 20px rgba(0,0,0,0.05); }}
  .header {{ text-align: center; margin-bottom: 25px; }}
  .bill-no {{ color: #4f46e5; font-weight: bold; font-family: monospace; }}
  .amount {{ font-size: 32px; font-weight: bold; color: #1e293b; margin: 20px 0; }}
  .item {{ display: flex; padding: 10px 0; border-bottom: 1px solid #f1f5f9; }}
</style>
</head>
<body>
  <div class='card'>
    <div class='header'>
      <div style='font-size: 24px; font-weight: bold;'>Invoiced Amount</div>
      <div class='amount'>₹{amount}</div>
      <div class='bill-no'>Bill Reference: {billNumber}</div>
    </div>
    <div class='content'>
      <p>Hello {patientName},</p>
      <p>Your invoice for the medical services provided on {date} has been generated. The status is currently <strong>{status}</strong>.</p>
      <div style='background:#f8fafc; padding:15px; border-radius:8px;'>
         <div style='display:block; overflow:hidden;'>
            <span style='float:left;'>Description</span><span style='float:right;'>Consultation & Services</span>
         </div>
         <div style='display:block; overflow:hidden; margin-top:10px;'>
            <span style='float:left;'>Total Due</span><strong style='float:right;'>₹{amount}</strong>
         </div>
      </div>
      <p style='margin-top:20px; font-size:14px; color:#64748b;'>Thank you for choosing MediCore Hospitals.</p>
    </div>
    {_hospitalSignature}
  </div>
</body>
</html>";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendLabReportAsync(string toEmail, string patientName, string testType, string results, string date, string reportUrl)
        {
            var subject = $"Diagnostic Report Available — {testType}";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
<style>
  body {{ font-family: sans-serif; background: #f0fdf4; padding: 20px; }}
  .card {{ background: white; max-width: 550px; margin: auto; padding: 30px; border-radius: 12px; border-top: 5px solid #10b981; }}
  .header {{ border-bottom: 1px solid #f1f5f9; padding-bottom: 15px; margin-bottom: 20px; }}
</style>
</head>
<body>
  <div class='card'>
    <div class='header'>
      <h2 style='color:#065f46; margin:0;'>Diagnostic Report View</h2>
      <p style='color:#6b7280; font-size:14px;'>Date: {date}</p>
    </div>
    <div class='content'>
      <p>Hello {patientName},</p>
      <p>The results for your <strong>{testType}</strong> are now available.</p>
      <div style='background:#f9fafb; padding:20px; border-radius:10px; border:1px solid #e5e7eb; margin:20px 0;'>
        <strong style='font-size:12px; color:#6b7280; text-transform:uppercase;'>Technician Observations:</strong>
        <p style='margin-top:10px; line-height:1.6;'>{results}</p>
      </div>
      {(string.IsNullOrEmpty(reportUrl) ? "" : $"<a href='{reportUrl}' style='display:inline-block; background:#10b981; color:white; padding:12px 24px; border-radius:8px; text-decoration:none; font-weight:bold;'>Download Full PDF Report</a>")}
    </div>
    {_hospitalSignature}
  </div>
</body>
</html>";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendDailyDigestAsync(string adminEmail, string adminName, int totalPatients, decimal totalRevenue, int pendingAppts, int tomorrowCount, string dietPlanHtml = "")
        {
            var today = DateTime.Now.ToString("dddd, dd MMMM yyyy");
            var subject = $"🏥 MediCore Daily Digest — {DateTime.Now:dd MMM yyyy}";

            var dietSection = string.IsNullOrEmpty(dietPlanHtml) ? "" : $@"
      <div style='margin-top:32px; border-radius:16px; overflow:hidden; border:1px solid #d1fae5;'>
        <div style='background:linear-gradient(135deg,#065f46,#047857); padding:16px 24px;'>
          <div style='color:#a7f3d0; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:1.5px; margin-bottom:4px;'>Optional Wellness Note</div>
          <div style='color:#ffffff; font-size:17px; font-weight:700;'>🥦 Doctor's Diet Plan of the Day</div>
        </div>
        <div style='background:#f0fdf4; padding:20px 24px; font-size:14px; color:#166534; line-height:1.8;'>{dietPlanHtml}</div>
      </div>";

            var body = $@"<!DOCTYPE html><html><head><meta charset='UTF-8'>
<style>
  body{{margin:0;padding:0;background:#f1f5f9;font-family:'Segoe UI',Arial,sans-serif;}}
  .wrap{{max-width:640px;margin:40px auto;}}
  .card{{background:#fff;border-radius:20px;overflow:hidden;box-shadow:0 8px 32px rgba(0,0,0,0.08);border:1px solid #e2e8f0;}}
  .hdr{{background:linear-gradient(135deg,#0f172a 0%,#1e3a5f 100%);padding:40px;text-align:center;}}
  .hdr .logo{{font-size:36px;margin-bottom:12px;}}
  .hdr h1{{color:#fff;margin:0;font-size:26px;font-weight:800;letter-spacing:-0.5px;}}
  .hdr p{{color:#94a3b8;margin:6px 0 0;font-size:14px;}}
  .body{{padding:32px 40px;}}
  .greeting{{font-size:16px;color:#334155;margin-bottom:24px;}}
  .stats-grid{{display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:24px;}}
  .stat-card{{border-radius:14px;padding:20px;text-align:center;}}
  .stat-card.blue{{background:linear-gradient(135deg,#dbeafe,#eff6ff);border:1px solid #bfdbfe;}}
  .stat-card.green{{background:linear-gradient(135deg,#dcfce7,#f0fdf4);border:1px solid #bbf7d0;}}
  .stat-card.amber{{background:linear-gradient(135deg,#fef3c7,#fffbeb);border:1px solid #fde68a;}}
  .stat-card.purple{{background:linear-gradient(135deg,#ede9fe,#f5f3ff);border:1px solid #ddd6fe;}}
  .stat-icon{{font-size:28px;margin-bottom:8px;}}
  .stat-val{{font-size:32px;font-weight:800;margin:4px 0;}}
  .stat-card.blue .stat-val{{color:#1d4ed8;}}
  .stat-card.green .stat-val{{color:#15803d;}}
  .stat-card.amber .stat-val{{color:#b45309;}}
  .stat-card.purple .stat-val{{color:#6d28d9;}}
  .stat-lbl{{font-size:12px;font-weight:600;text-transform:uppercase;letter-spacing:0.5px;color:#64748b;}}
  .section-title{{font-size:11px;font-weight:700;text-transform:uppercase;letter-spacing:1px;color:#94a3b8;margin-bottom:12px;}}
  .alert-row{{background:#fef3c7;border:1px solid #fde68a;border-radius:10px;padding:14px 18px;margin-bottom:24px;display:flex;align-items:center;gap:12px;}}
  .alert-row .icon{{font-size:24px;}}
  .alert-row p{{margin:0;font-size:14px;color:#92400e;font-weight:600;}}
  .footer{{background:#f8fafc;padding:24px 40px;text-align:center;border-top:1px solid #f1f5f9;}}
  .footer p{{margin:0;color:#94a3b8;font-size:12px;line-height:1.6;}}
</style></head><body>
<div class='wrap'>
  <div class='card'>
    <div class='hdr'>
      <div class='logo'>🏥</div>
      <h1>Daily Hospital Digest</h1>
      <p>{today}</p>
    </div>
    <div class='body'>
      <div class='greeting'>Good evening, <strong>{adminName}</strong>. Here's your end-of-day summary for MediCore Hospital.</div>
      
      <div class='section-title'>Today's Performance</div>
      <div class='stats-grid'>
        <div class='stat-card blue'>
          <div class='stat-icon'>👥</div>
          <div class='stat-val'>{totalPatients}</div>
          <div class='stat-lbl'>Patients Today</div>
        </div>
        <div class='stat-card green'>
          <div class='stat-icon'>💰</div>
          <div class='stat-val'>₹{totalRevenue:N0}</div>
          <div class='stat-lbl'>Revenue Today</div>
        </div>
        <div class='stat-card amber'>
          <div class='stat-icon'>⏳</div>
          <div class='stat-val'>{pendingAppts}</div>
          <div class='stat-lbl'>Pending Appointments</div>
        </div>
        <div class='stat-card purple'>
          <div class='stat-icon'>📅</div>
          <div class='stat-val'>{tomorrowCount}</div>
          <div class='stat-lbl'>Tomorrow's Bookings</div>
        </div>
      </div>

      {(pendingAppts > 0 ? $@"<div class='alert-row'>
        <div class='icon'>⚠️</div>
        <p>There are {pendingAppts} pending appointments that may need follow-up or rescheduling.</p>
      </div>" : "")}

      {dietSection}

      {_hospitalSignature}
    </div>
    <div class='footer'>
      <p>This digest is automatically generated every evening by the MediCore HMS.</p>
      <p style='margin-top:8px;'>© 2026 MediCore Hospitals. All rights reserved.</p>
    </div>
  </div>
</div>
</body></html>";

            await SendEmailAsync(adminEmail, subject, body);
        }

        public async Task SendAppointmentReminderAsync(string toEmail, string patientName, string doctorName, string department, string date, string time, string tokenNumber, decimal fee)
        {
            var subject = $"⏰ Reminder: Your Appointment Tomorrow with {doctorName}";

            var body = $@"<!DOCTYPE html><html><head><meta charset='UTF-8'>
<style>
  body{{margin:0;padding:0;background:#f8fafc;font-family:'Segoe UI',Arial,sans-serif;}}
  .wrap{{max-width:600px;margin:40px auto;}}
  .card{{background:#fff;border-radius:20px;overflow:hidden;box-shadow:0 10px 40px rgba(0,0,0,0.08);border:1px solid #e2e8f0;}}
  .hdr{{background:linear-gradient(135deg,#7c3aed 0%,#4f46e5 100%);padding:44px 40px;text-align:center;}}
  .hdr .icon{{font-size:48px;margin-bottom:16px;}}
  .hdr h1{{color:#fff;margin:0;font-size:26px;font-weight:800;}}
  .hdr p{{color:#c4b5fd;margin:8px 0 0;font-size:14px;}}
  .body{{padding:36px 40px;}}
  .greeting{{font-size:17px;color:#1e293b;font-weight:600;margin-bottom:20px;}}
  .appt-box{{background:linear-gradient(135deg,#f5f3ff,#ede9fe);border:1.5px solid #ddd6fe;border-radius:16px;padding:24px;margin-bottom:24px;}}
  .appt-row{{display:flex;justify-content:space-between;align-items:center;padding:10px 0;border-bottom:1px solid rgba(109,40,217,0.1);}}
  .appt-row:last-child{{border-bottom:none;}}
  .appt-label{{color:#6d28d9;font-size:13px;font-weight:600;}}
  .appt-val{{color:#1e293b;font-size:14px;font-weight:700;text-align:right;}}
  .token-box{{background:linear-gradient(135deg,#0f172a,#1e293b);border-radius:14px;padding:24px;text-align:center;margin-bottom:24px;}}
  .token-lbl{{color:#94a3b8;font-size:11px;font-weight:700;letter-spacing:2px;text-transform:uppercase;margin-bottom:8px;}}
  .token-val{{color:#fff;font-size:52px;font-weight:900;letter-spacing:4px;}}
  .checklist{{background:#f0fdf4;border:1px solid #bbf7d0;border-radius:14px;padding:20px 24px;margin-bottom:24px;}}
  .checklist h3{{color:#166534;font-size:14px;font-weight:700;margin:0 0 14px 0;}}
  .check-item{{display:flex;align-items:flex-start;gap:10px;margin-bottom:10px;font-size:13px;color:#166534;}}
  .check-icon{{font-size:16px;flex-shrink:0;margin-top:1px;}}
  .map-btn{{display:block;background:linear-gradient(135deg,#1d4ed8,#3b82f6);color:#fff;text-decoration:none;padding:14px;border-radius:12px;text-align:center;font-weight:700;font-size:14px;margin-bottom:24px;}}
  .footer{{background:#f8fafc;padding:24px 40px;text-align:center;border-top:1px solid #f1f5f9;}}
  .footer p{{margin:0;color:#94a3b8;font-size:12px;line-height:1.6;}}
</style></head><body>
<div class='wrap'>
  <div class='card'>
    <div class='hdr'>
      <div class='icon'>⏰</div>
      <h1>Appointment Tomorrow</h1>
      <p>Don't forget — you have a scheduled visit</p>
    </div>
    <div class='body'>
      <div class='greeting'>Hello {patientName}, this is a friendly reminder about your appointment tomorrow!</div>

      <div class='appt-box'>
        <div class='appt-row'><span class='appt-label'>Doctor</span><span class='appt-val'>Dr. {doctorName}</span></div>
        <div class='appt-row'><span class='appt-label'>Department</span><span class='appt-val'>{department}</span></div>
        <div class='appt-row'><span class='appt-label'>Date</span><span class='appt-val'>{date}</span></div>
        <div class='appt-row'><span class='appt-label'>Time Slot</span><span class='appt-val'>{time}</span></div>
        <div class='appt-row'><span class='appt-label'>Consultation Fee</span><span class='appt-val'>₹{fee}</span></div>
      </div>

      <div class='token-box'>
        <div class='token-lbl'>Your Queue Token Number</div>
        <div class='token-val'>{tokenNumber}</div>
      </div>

      <div class='checklist'>
        <h3>✅ What to Bring Tomorrow</h3>
        <div class='check-item'><span class='check-icon'>🪪</span><span>Government-issued Photo ID (Aadhaar / Passport)</span></div>
        <div class='check-item'><span class='check-icon'>📋</span><span>Previous medical records, prescriptions & reports</span></div>
        <div class='check-item'><span class='check-icon'>💊</span><span>Current medications list (if any)</span></div>
        <div class='check-item'><span class='check-icon'>💳</span><span>Payment method (Cash / Card / UPI) for consultation fee</span></div>
        <div class='check-item'><span class='check-icon'>⏱️</span><span>Arrive 15 minutes early to complete registration</span></div>
        <div class='check-item'><span class='check-icon'>🚫</span><span>Avoid eating 2 hours before if blood tests are expected</span></div>
      </div>

      <a class='map-btn' href='https://www.google.com/maps/search/MediCore+Hospital+Hyderabad' target='_blank'>
        📍 Get Directions to MediCore Hospital
      </a>

      {_hospitalSignature}
    </div>
    <div class='footer'>
      <p>To reschedule or cancel, please contact us or use the patient portal at least 24 hours before your appointment.</p>
      <p style='margin-top:8px;'>© 2026 MediCore Hospitals. All rights reserved.</p>
    </div>
  </div>
</div>
</body></html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var smtpHost = _config["Email:SmtpHost"];
            var smtpPortStr = _config["Email:SmtpPort"];
            var smtpUser = _config["Email:SmtpUser"];
            var smtpPass = _config["Email:SmtpPass"];
            var fromEmail = _config["Email:FromEmail"];
            var fromName = _config["Email:FromName"] ?? "MediCore Hospital";

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPortStr) || string.IsNullOrEmpty(smtpUser))
            {
                throw new InvalidOperationException("SMTP configuration is missing in appsettings.json. Please configure Email:SmtpHost, Email:SmtpPort, and Email:SmtpUser.");
            }

            int smtpPort = int.Parse(smtpPortStr);

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}