using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Identity.API.Controllers
{
    [ApiController]
    [Route("api/marketing")]
    public class MarketingController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MarketingController> _logger;

        public MarketingController(IConfiguration configuration, ILogger<MarketingController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("demo-request")]
        public async Task<IActionResult> BookDemo([FromBody] DemoRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.ContactName))
            {
                return BadRequest("Invalid request data.");
            }

            _logger.LogInformation("[MARKETING] Demo Request received from {ContactName} for business {BusinessName} ({Email})", request.ContactName, request.BusinessName, request.Email);

            // Construct email details for sales team
            string subject = $"New Demo Booking: {request.BusinessName}";
            string body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <h2 style='color: #3b82f6;'>New Demo Request Received!</h2>
                    <p>A prospective customer has requested a live demo of OurStock.</p>
                    <table style='border-collapse: collapse; width: 100%; max-width: 600px; margin-top: 15px;'>
                        <tr style='background-color: #f3f4f6;'>
                            <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold; width: 180px;'>Business Name:</td>
                            <td style='padding: 10px; border: 1px solid #e5e7eb;'>{request.BusinessName}</td>
                        </tr>
                        <tr>
                            <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>Contact Name:</td>
                            <td style='padding: 10px; border: 1px solid #e5e7eb;'>{request.ContactName}</td>
                        </tr>
                        <tr style='background-color: #f3f4f6;'>
                            <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>Email Address:</td>
                            <td style='padding: 10px; border: 1px solid #e5e7eb;'>{request.Email}</td>
                        </tr>
                        <tr>
                            <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>Phone Number:</td>
                            <td style='padding: 10px; border: 1px solid #e5e7eb;'>{request.Phone}</td>
                        </tr>
                        <tr style='background-color: #f3f4f6;'>
                            <td style='padding: 10px; border: 1px solid #e5e7eb; font-weight: bold;'>Business Category:</td>
                            <td style='padding: 10px; border: 1px solid #e5e7eb;'>{request.BusinessType}</td>
                        </tr>
                    </table>
                    <br/>
                    <p>Please contact the prospect within 24 hours to schedule their Zoom demo.</p>
                    <hr style='border: none; border-top: 1px solid #e5e7eb;'/>
                    <p style='font-size: 12px; color: #94a3b8;'>OurStock Marketing Automation System</p>
                </body>
                </html>";

            // Construct thank you email for customer
            string customerSubject = "Your OurStock Live Demo Request Confirmed!";
            string customerBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <h2 style='color: #3b82f6;'>Hello {request.ContactName},</h2>
                    <p>Thank you for requesting a personalized live demo of OurStock ERP!</p>
                    <p>We are excited to show you how OurStock can help manage your inventory, GST billing, payrolls, and accounting ledger sheets.</p>
                    <p>One of our product experts will contact you at <strong>{request.Phone}</strong> within 24 hours to schedule a convenient time for your Zoom tour.</p>
                    <br/>
                    <p>Best regards,</p>
                    <p><strong>The OurStock Team</strong></p>
                    <hr style='border: none; border-top: 1px solid #e5e7eb;'/>
                    <p style='font-size: 12px; color: #94a3b8;'>OurStock Systems Ltd. | Made in India</p>
                </body>
                </html>";

            // SMTP Setup
            var smtpSection = _configuration.GetSection("SmtpSettings");
            string smtpHost = smtpSection["Host"];
            string smtpPortStr = smtpSection["Port"];
            string smtpEmail = smtpSection["Email"];
            string smtpPassword = smtpSection["Password"];
            string smtpUseSslStr = smtpSection["UseSsl"];
            string notifyEmail = smtpSection["NotifyEmail"] ?? "sales@ourstock.in";

            bool isSmtpConfigured = !string.IsNullOrEmpty(smtpHost) && !string.IsNullOrEmpty(smtpEmail) && !string.IsNullOrEmpty(smtpPassword);

            if (isSmtpConfigured)
            {
                try
                {
                    int smtpPort = int.TryParse(smtpPortStr, out var p) ? p : 587;
                    bool smtpUseSsl = bool.TryParse(smtpUseSslStr, out var ssl) ? ssl : true;

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                    using (var smtp = new SmtpClient(smtpHost, smtpPort))
                    {
                        smtp.EnableSsl = smtpUseSsl;
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(smtpEmail, smtpPassword);
                        smtp.Timeout = 15000;

                        // 1. Send email to Sales notification address
                        using (var messageToSales = new MailMessage(smtpEmail, notifyEmail)
                        {
                            Subject = subject,
                            Body = body,
                            IsBodyHtml = true
                        })
                        {
                            await smtp.SendMailAsync(messageToSales);
                        }

                        // 2. Send confirmation email to Customer
                        using (var messageToClient = new MailMessage(smtpEmail, request.Email)
                        {
                            Subject = customerSubject,
                            Body = customerBody,
                            IsBodyHtml = true
                        })
                        {
                            await smtp.SendMailAsync(messageToClient);
                        }
                    }
                    _logger.LogInformation("[MARKETING] Real emails successfully sent via SMTP to: {Customer} and {Sales}", request.Email, notifyEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[MARKETING] Real email delivery failed via SMTP. Falling back to log simulation.");
                }
            }
            else
            {
                _logger.LogWarning("[MARKETING] SmtpSettings not found in configuration. Real email skipped.");
            }

            // Always write simulated email delivery logs for validation and dev visibility
            try
            {
                string logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                string logFilePath = Path.Combine(logDir, "demo_booking_emails.log");

                string logContent = $"========================================\n" +
                                    $"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                                    $"SMTP Configured: {isSmtpConfigured}\n" +
                                    $"Recipient (Sales): {notifyEmail}\n" +
                                    $"Recipient (Client): {request.Email}\n" +
                                    $"Sales Email Subject: {subject}\n" +
                                    $"Sales Email Body:\n{body}\n" +
                                    $"Client Email Subject: {customerSubject}\n" +
                                    $"Client Email Body:\n{customerBody}\n" +
                                    $"========================================\n\n";

                await System.IO.File.AppendAllTextAsync(logFilePath, logContent);
                
                // Write standard Console output for Docker log visibility
                Console.WriteLine("\n=== [SIMULATED EMAIL DISPATCH] ===");
                Console.WriteLine($"From: {smtpEmail ?? "no-reply@ourstock.in"}");
                Console.WriteLine($"To Sales: {notifyEmail}");
                Console.WriteLine($"To Customer: {request.Email}");
                Console.WriteLine($"Customer Subject: {customerSubject}");
                Console.WriteLine($"Customer Body: {customerBody}");
                Console.WriteLine("===================================\n");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MARKETING] Failed to write email simulation logs.");
            }

            return Ok(new { Success = true, Message = "Demo request registered successfully." });
        }
    }

    public class DemoRequestDto
    {
        public string BusinessName { get; set; }
        public string ContactName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string BusinessType { get; set; }
    }
}
