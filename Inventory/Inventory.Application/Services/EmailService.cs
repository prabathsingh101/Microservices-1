using System.Net;
using System.Net.Mail;
using System.Security.Authentication;
using Inventory.Application.Clients.DTOs;

namespace Inventory.Application.Services
{
    public class EmailService : IEmailService
    {
        public async Task SendPoEmailAsync(CompanyProfileDto company, string supplierEmail, string poNumber, decimal amount, byte[] pdfAttachmentBytes = null)
        {
            if (string.IsNullOrEmpty(company.SmtpHost) || string.IsNullOrEmpty(company.SmtpEmail) || string.IsNullOrEmpty(company.SmtpPassword))
            {
                Console.WriteLine("[EmailService] SMTP settings missing. Skipping email.");
                return;
            }

            if (string.IsNullOrEmpty(supplierEmail))
            {
                Console.WriteLine("[EmailService] Supplier email missing. Skipping email.");
                return;
            }

            try
            {
                // Ensure TLS 1.2 or 1.3 is used for secure connection
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                var fromAddress = new MailAddress(company.SmtpEmail, company.Name);
                var toAddress = new MailAddress(supplierEmail);
                string subject = $"Purchase Order: {poNumber} from {company.Name}";
                string body = $@"<html><body><h2>Dear Supplier,</h2><p>We are pleased to place a New Purchase Order with you.</p><p><strong>PO Number:</strong> {poNumber}</p><p><strong>Total Amount:</strong> {amount}</p><p>Please find the details in the attached document (coming soon) or log in to our portal.</p><br/><p>Regards,</p><p><strong>{company.Name}</strong></p></body></html>";

                Console.WriteLine($"[EmailService] Attempting to send PO email via {company.SmtpHost}:{company.SmtpPort} with SSL: {company.SmtpUseSsl}");

                using (var smtp = new SmtpClient(company.SmtpHost, company.SmtpPort ?? 587))
                {
                    smtp.EnableSsl = company.SmtpUseSsl;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(company.SmtpEmail, company.SmtpPassword);
                    smtp.Timeout = 20000;

                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        Console.WriteLine($"[EmailService] Sending PO Email. Attachment bytes length: {(pdfAttachmentBytes != null ? pdfAttachmentBytes.Length.ToString() : "NULL")}");

                        if (pdfAttachmentBytes != null && pdfAttachmentBytes.Length > 0)
                        {
                            using (var ms = new System.IO.MemoryStream(pdfAttachmentBytes))
                            {
                                var attachment = new Attachment(ms, $"PurchaseOrder_{poNumber.Replace("/", "_")}.pdf", "application/pdf");
                                message.Attachments.Add(attachment);
                                await smtp.SendMailAsync(message);
                            }
                        }
                        else
                        {
                            await smtp.SendMailAsync(message);
                        }
                    }
                }
                Console.WriteLine($"[EmailService] PO Email sent to {supplierEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] PO Email fail: {ex.Message} | {ex.InnerException?.Message}");
            }
        }

        public async Task SendSoEmailAsync(CompanyProfileDto company, string customerEmail, string soNumber, decimal amount, byte[] pdfAttachmentBytes = null)
        {
            if (string.IsNullOrEmpty(company.SmtpHost) || string.IsNullOrEmpty(company.SmtpEmail) || string.IsNullOrEmpty(company.SmtpPassword))
            {
                Console.WriteLine("[EmailService] SMTP settings missing. Skipping SO email.");
                return;
            }

            if (string.IsNullOrEmpty(customerEmail))
            {
                Console.WriteLine("[EmailService] Customer email missing. Skipping SO email.");
                return;
            }

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                var fromAddress = new MailAddress(company.SmtpEmail, company.Name);
                var toAddress = new MailAddress(customerEmail);
                string subject = $"Sale Order Confirmation: {soNumber} - {company.Name}";
                string body = $@"<html><body><h2>Dear Customer,</h2><p>Thank you for your order!</p><p><strong>Order Number:</strong> {soNumber}</p><p><strong>Total Amount:</strong> {amount}</p><p>We are processing your order and will notify you once it's shipped. Please find your tax invoice attached.</p><br/><p>Regards,</p><p><strong>{company.Name}</strong></p></body></html>";

                Console.WriteLine($"[EmailService] Attempting to send SO email via {company.SmtpHost}:{company.SmtpPort} with SSL: {company.SmtpUseSsl}");

                using (var smtp = new SmtpClient(company.SmtpHost, company.SmtpPort ?? 587))
                {
                    smtp.EnableSsl = company.SmtpUseSsl;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(company.SmtpEmail, company.SmtpPassword);
                    smtp.Timeout = 20000;

                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        if (pdfAttachmentBytes != null && pdfAttachmentBytes.Length > 0)
                        {
                            using (var ms = new System.IO.MemoryStream(pdfAttachmentBytes))
                            {
                                var attachment = new Attachment(ms, $"Invoice_{soNumber}.pdf", "application/pdf");
                                message.Attachments.Add(attachment);
                                await smtp.SendMailAsync(message);
                            }
                        }
                        else
                        {
                            await smtp.SendMailAsync(message);
                        }
                    }
                }
                Console.WriteLine($"[EmailService] SO Email sent to {customerEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] SO Email fail: {ex.Message} | {ex.InnerException?.Message}");
            }
        }
        
        public async Task SendGrnEmailAsync(CompanyProfileDto company, string supplierEmail, string grnNumber, string poNumber, decimal amount, byte[] pdfAttachmentBytes = null)
        {
            if (string.IsNullOrEmpty(company.SmtpHost) || string.IsNullOrEmpty(company.SmtpEmail) || string.IsNullOrEmpty(company.SmtpPassword))
            {
                Console.WriteLine("[EmailService] SMTP settings missing. Skipping GRN email.");
                return;
            }

            if (string.IsNullOrEmpty(supplierEmail))
            {
                Console.WriteLine("[EmailService] Supplier email missing. Skipping GRN email.");
                return;
            }

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                var fromAddress = new MailAddress(company.SmtpEmail, company.Name);
                var toAddress = new MailAddress(supplierEmail);
                string subject = $"Goods Received Advice: {grnNumber} (Ref PO: {poNumber}) - {company.Name}";
                string body = $@"<html><body><h2>Dear Supplier,</h2><p>This is to inform you that we have received the goods against your supply.</p><p><strong>GRN Number:</strong> {grnNumber}</p><p><strong>PO Reference:</strong> {poNumber}</p><p><strong>Accepted Amount:</strong> {amount}</p><p>The inventory has been updated in our system. Thank you for the timely delivery.</p><br/><p>Regards,</p><p><strong>{company.Name}</strong></p></body></html>";

                Console.WriteLine($"[EmailService] Attempting to send GRN email via {company.SmtpHost}:{company.SmtpPort} with SSL: {company.SmtpUseSsl}");

                using (var smtp = new SmtpClient(company.SmtpHost, company.SmtpPort ?? 587))
                {
                    smtp.EnableSsl = company.SmtpUseSsl;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(company.SmtpEmail, company.SmtpPassword);
                    smtp.Timeout = 20000;

                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        Console.WriteLine($"[EmailService] Sending GRN Email. Attachment bytes length: {(pdfAttachmentBytes != null ? pdfAttachmentBytes.Length.ToString() : "NULL")}");

                        if (pdfAttachmentBytes != null && pdfAttachmentBytes.Length > 0)
                        {
                            using (var ms = new System.IO.MemoryStream(pdfAttachmentBytes))
                            {
                                var attachment = new Attachment(ms, $"GRN_{grnNumber.Replace("/", "_")}.pdf", "application/pdf");
                                message.Attachments.Add(attachment);
                                await smtp.SendMailAsync(message);
                            }
                        }
                        else
                        {
                            await smtp.SendMailAsync(message);
                        }
                    }
                }
                Console.WriteLine($"[EmailService] GRN Email sent to {supplierEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] GRN Email fail: {ex.Message} | {ex.InnerException?.Message}");
            }
        }

        public async Task SendCancelledGrnEmailAsync(CompanyProfileDto company, string supplierEmail, string grnNumber, string poNumber, decimal amount, string reason, byte[] pdfAttachmentBytes = null)
        {
            if (string.IsNullOrEmpty(company.SmtpHost) || string.IsNullOrEmpty(company.SmtpEmail) || string.IsNullOrEmpty(company.SmtpPassword))
            {
                Console.WriteLine("[EmailService] SMTP settings missing. Skipping Cancelled GRN email.");
                return;
            }

            if (string.IsNullOrEmpty(supplierEmail))
            {
                Console.WriteLine("[EmailService] Supplier email missing. Skipping Cancelled GRN email.");
                return;
            }

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                var fromAddress = new MailAddress(company.SmtpEmail, company.Name);
                var toAddress = new MailAddress(supplierEmail);
                string subject = $"CANCELLED: Goods Received Advice: {grnNumber} (Ref PO: {poNumber}) - {company.Name}";
                string body = $@"<html><body><h2>Dear Supplier,</h2><p>This is to notify you that the Goods Received Advice (GRN) listed below has been <strong>CANCELLED</strong>.</p><p><strong>GRN Number:</strong> {grnNumber}</p><p><strong>PO Reference:</strong> {poNumber}</p><p><strong>Cancelled Amount:</strong> {amount}</p><p><strong>Reason for Cancellation:</strong> {reason}</p><p>If you have any questions or concern, please contact us immediately. Please find the cancelled document attached.</p><br/><p>Regards,</p><p><strong>{company.Name}</strong></p></body></html>";

                Console.WriteLine($"[EmailService] Attempting to send Cancelled GRN email via {company.SmtpHost}:{company.SmtpPort}");

                using (var smtp = new SmtpClient(company.SmtpHost, company.SmtpPort ?? 587))
                {
                    smtp.EnableSsl = company.SmtpUseSsl;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(company.SmtpEmail, company.SmtpPassword);
                    smtp.Timeout = 20000;

                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        if (pdfAttachmentBytes != null && pdfAttachmentBytes.Length > 0)
                        {
                            using (var ms = new System.IO.MemoryStream(pdfAttachmentBytes))
                            {
                                var attachment = new Attachment(ms, $"Cancelled_GRN_{grnNumber.Replace("/", "_")}.pdf", "application/pdf");
                                message.Attachments.Add(attachment);
                                await smtp.SendMailAsync(message);
                            }
                        }
                        else
                        {
                            await smtp.SendMailAsync(message);
                        }
                    }
                }
                Console.WriteLine($"[EmailService] Cancelled GRN Email sent to {supplierEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] Cancelled GRN Email fail: {ex.Message} | {ex.InnerException?.Message}");
            }
        }

        public async Task SendCancelledInvoiceEmailAsync(CompanyProfileDto company, string customerEmail, string invoiceNumber, decimal amount, string reason, byte[] pdfAttachmentBytes = null)
        {
            if (string.IsNullOrEmpty(company.SmtpHost) || string.IsNullOrEmpty(company.SmtpEmail) || string.IsNullOrEmpty(company.SmtpPassword))
            {
                Console.WriteLine("[EmailService] SMTP settings missing. Skipping Cancelled Invoice email.");
                return;
            }

            if (string.IsNullOrEmpty(customerEmail))
            {
                Console.WriteLine("[EmailService] Customer email missing. Skipping Cancelled Invoice email.");
                return;
            }

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                var fromAddress = new MailAddress(company.SmtpEmail, company.Name);
                var toAddress = new MailAddress(customerEmail);
                string subject = $"CANCELLED: Tax Invoice: {invoiceNumber} - {company.Name}";
                string body = $@"<html><body><h2>Dear Customer,</h2><p>This is to notify you that your Tax Invoice has been <strong>CANCELLED</strong>.</p><p><strong>Invoice Number:</strong> {invoiceNumber}</p><p><strong>Amount Reversed:</strong> {amount}</p><p><strong>Reason for Cancellation:</strong> {reason}</p><p>A refund or ledger reversal has been posted. Please find the cancelled invoice details attached.</p><br/><p>Regards,</p><p><strong>{company.Name}</strong></p></body></html>";

                Console.WriteLine($"[EmailService] Attempting to send Cancelled Invoice email via {company.SmtpHost}:{company.SmtpPort}");

                using (var smtp = new SmtpClient(company.SmtpHost, company.SmtpPort ?? 587))
                {
                    smtp.EnableSsl = company.SmtpUseSsl;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(company.SmtpEmail, company.SmtpPassword);
                    smtp.Timeout = 20000;

                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        if (pdfAttachmentBytes != null && pdfAttachmentBytes.Length > 0)
                        {
                            using (var ms = new System.IO.MemoryStream(pdfAttachmentBytes))
                            {
                                var attachment = new Attachment(ms, $"Cancelled_Invoice_{invoiceNumber.Replace("/", "_")}.pdf", "application/pdf");
                                message.Attachments.Add(attachment);
                                await smtp.SendMailAsync(message);
                            }
                        }
                        else
                        {
                            await smtp.SendMailAsync(message);
                        }
                    }
                }
                Console.WriteLine($"[EmailService] Cancelled Invoice Email sent to {customerEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] Cancelled Invoice Email fail: {ex.Message} | {ex.InnerException?.Message}");
            }
        }

        public async Task SendCancelledSaleOrderEmailAsync(CompanyProfileDto company, string customerEmail, string soNumber, decimal amount, string reason, byte[] pdfAttachmentBytes = null)
        {
            if (string.IsNullOrEmpty(company.SmtpHost) || string.IsNullOrEmpty(company.SmtpEmail) || string.IsNullOrEmpty(company.SmtpPassword))
            {
                Console.WriteLine("[EmailService] SMTP settings missing. Skipping Cancelled Sale Order email.");
                return;
            }

            if (string.IsNullOrEmpty(customerEmail))
            {
                Console.WriteLine("[EmailService] Customer email missing. Skipping Cancelled Sale Order email.");
                return;
            }

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                var fromAddress = new MailAddress(company.SmtpEmail, company.Name);
                var toAddress = new MailAddress(customerEmail);
                string subject = $"CANCELLED: Sale Order: {soNumber} - {company.Name}";
                string body = $@"<html><body><h2>Dear Customer,</h2><p>This is to notify you that your Sale Order has been <strong>CANCELLED</strong>.</p><p><strong>Order Number:</strong> {soNumber}</p><p><strong>Order Amount:</strong> {amount}</p><p><strong>Reason for Cancellation:</strong> {reason}</p><p>Please find the cancelled order details attached.</p><br/><p>Regards,</p><p><strong>{company.Name}</strong></p></body></html>";

                Console.WriteLine($"[EmailService] Attempting to send Cancelled Sale Order email via {company.SmtpHost}:{company.SmtpPort}");

                using (var smtp = new SmtpClient(company.SmtpHost, company.SmtpPort ?? 587))
                {
                    smtp.EnableSsl = company.SmtpUseSsl;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(company.SmtpEmail, company.SmtpPassword);
                    smtp.Timeout = 20000;

                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        if (pdfAttachmentBytes != null && pdfAttachmentBytes.Length > 0)
                        {
                            using (var ms = new System.IO.MemoryStream(pdfAttachmentBytes))
                            {
                                var attachment = new Attachment(ms, $"Cancelled_SaleOrder_{soNumber.Replace("/", "_")}.pdf", "application/pdf");
                                message.Attachments.Add(attachment);
                                await smtp.SendMailAsync(message);
                            }
                        }
                        else
                        {
                            await smtp.SendMailAsync(message);
                        }
                    }
                }
                Console.WriteLine($"[EmailService] Cancelled Sale Order Email sent to {customerEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] Cancelled Sale Order Email fail: {ex.Message} | {ex.InnerException?.Message}");
            }
        }
    }
}
