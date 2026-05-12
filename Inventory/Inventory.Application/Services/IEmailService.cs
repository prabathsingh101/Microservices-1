using Inventory.Application.Clients.DTOs;

namespace Inventory.Application.Services
{
    public interface IEmailService
    {
        Task SendPoEmailAsync(CompanyProfileDto company, string supplierEmail, string poNumber, decimal amount);
        Task SendSoEmailAsync(CompanyProfileDto company, string customerEmail, string soNumber, decimal amount, byte[] pdfAttachmentBytes = null);
        Task SendGrnEmailAsync(CompanyProfileDto company, string supplierEmail, string grnNumber, string poNumber, decimal amount);
    }
}
