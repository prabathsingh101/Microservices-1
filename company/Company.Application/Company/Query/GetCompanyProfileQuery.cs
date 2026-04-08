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
               
                new AddressDto(
                    data.CompanyAddress.Id,
                    data.CompanyAddress.AddressLine1,
                    data.CompanyAddress.AddressLine2,
                    data.CompanyAddress.City,
                    data.CompanyAddress.State,
                    data.CompanyAddress.StateCode, 
                    data.CompanyAddress.PinCode,
                    data.CompanyAddress.Country,
                    data.CompanyAddress.Email
                ),
               
                new BankDetailDto(
                    data.BankInformation.Id,
                    data.BankInformation.BankName,
                    data.BankInformation.BranchName,
                    data.BankInformation.AccountNumber,
                    data.BankInformation.IfscCode,
                    data.BankInformation.AccountType,
                    data.BankInformation.Email
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