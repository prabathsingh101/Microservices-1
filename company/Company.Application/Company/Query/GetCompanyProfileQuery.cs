using Company.Application.Common.Interfaces;
using Company.Application.DTOs;
using MediatR;

namespace Company.Application.Company.Queries
{
    // --- Query Definition ---
    public record GetCompanyProfileQuery() : IRequest<CompanyProfileDto?>;

    // --- Handler Definition ---
    public class GetCompanyProfileHandler : IRequestHandler<GetCompanyProfileQuery, CompanyProfileDto?>
    {
        private readonly ICompanyRepository _repo;

        public GetCompanyProfileHandler(ICompanyRepository repo)
        {
            _repo = repo; 
        }

        public async Task<CompanyProfileDto?> Handle(GetCompanyProfileQuery request, CancellationToken ct)
        {
           
            var data = await _repo.GetCompanyProfileAsync();

            if (data == null) return null;

           
            return new CompanyProfileDto(
                data.Id,
                data.Name,
                data.Tagline,
                data.RegistrationNumber,
                data.Gstin, // MaxLength 15
                data.LogoUrl,
                data.PrimaryEmail,
                data.Email,
                data.SmtpEmail,
                data.SmtpPassword,
                data.SmtpHost,
                data.SmtpPort,
                data.SmtpUseSsl,
                data.PrimaryPhone,
                data.Website,
                data.Message,
                data.DriverWhatsAppMessage,
                data.SaleReturnWindowValue,
                data.SaleReturnWindowUnit,
                data.SaleReturnPolicyDisclaimer,
                data.PurchaseReturnWindowValue,
                data.PurchaseReturnWindowUnit,
                data.PurchaseReturnPolicyDisclaimer,
                data.IsActive,
                data.InvoiceFooterMessage,
                data.EstimateFooterMessage,
                data.PurchaseOrderFooterMessage,
                data.SaleOrderFooterMessage,
                data.PurchaseOrderCreationMessage,
                data.PurchaseOrderStatusUpdateMessage,
                data.SaleOrderCreationMessage,
                data.SaleOrderConfirmationMessage,
               
                new AddressDto(
                    data.Addresses.FirstOrDefault()?.Id,
                    data.Addresses.FirstOrDefault()?.AddressLine1 ?? "",
                    data.Addresses.FirstOrDefault()?.AddressLine2 ?? "",
                    data.Addresses.FirstOrDefault()?.City ?? "",
                    data.Addresses.FirstOrDefault()?.State ?? "",
                    data.Addresses.FirstOrDefault()?.StateCode ?? "",
                    data.Addresses.FirstOrDefault()?.PinCode ?? "",
                    data.Addresses.FirstOrDefault()?.Country ?? "India",
                    data.Addresses.FirstOrDefault()?.Email
                ),
               
                new BankDetailDto(
                    data.BankDetails.FirstOrDefault()?.Id,
                    data.BankDetails.FirstOrDefault()?.BankName ?? "",
                    data.BankDetails.FirstOrDefault()?.BranchName ?? "",
                    data.BankDetails.FirstOrDefault()?.AccountNumber ?? "",
                    data.BankDetails.FirstOrDefault()?.IfscCode ?? "",
                    data.BankDetails.FirstOrDefault()?.AccountType ?? "Current",
                    data.BankDetails.FirstOrDefault()?.Email
                ),
                // Authorized Signatories mapping
                data.AuthorizedSignatories.Select(s => new AuthorizedSignatoryDto(
                    s.Id,
                    s.PersonName,
                    s.Designation,
                    s.SignatureImageUrl,
                    s.Email,
                    s.IsDefault
                )).ToList()
            );

        }
    }
}