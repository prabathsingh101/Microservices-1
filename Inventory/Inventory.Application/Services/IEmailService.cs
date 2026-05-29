using Inventory.Application.Clients.DTOs;

namespace Inventory.Application.Services
{
    public interface IEmailService
    {
        Task SendPoEmailAsync(CompanyProfileDto company, string supplierEmail, string poNumber, decimal amount, byte[] pdfAttachmentBytes = null);
        Task SendSoEmailAsync(CompanyProfileDto company, string customerEmail, string soNumber, decimal amount, byte[] pdfAttachmentBytes = null);
        Task SendGrnEmailAsync(CompanyProfileDto company, string supplierEmail, string grnNumber, string poNumber, decimal amount, byte[] pdfAttachmentBytes = null);
        Task SendCancelledGrnEmailAsync(CompanyProfileDto company, string supplierEmail, string grnNumber, string poNumber, decimal amount, string reason, byte[] pdfAttachmentBytes = null);
        Task SendCancelledInvoiceEmailAsync(CompanyProfileDto company, string customerEmail, string invoiceNumber, decimal amount, string reason, byte[] pdfAttachmentBytes = null);
    }
}
