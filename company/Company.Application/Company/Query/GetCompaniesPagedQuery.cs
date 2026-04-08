using Company.Application.Common.Interfaces;
using Company.Application.Common.Models;
using Company.Application.DTOs;
using MediatR;
using System.Linq;

namespace Company.Application.Company.Queries
{
    public record GetCompaniesPagedQuery(GridRequest Request) : IRequest<GridResponse<CompanyProfileDto>>;

    public class GetCompaniesPagedHandler : IRequestHandler<GetCompaniesPagedQuery, GridResponse<CompanyProfileDto>>
    {
        private readonly ICompanyRepository _repo;
        public GetCompaniesPagedHandler(ICompanyRepository repo) => _repo = repo;

        public async Task<GridResponse<CompanyProfileDto>> Handle(GetCompaniesPagedQuery request, CancellationToken ct)
        {
            var pagedData = await _repo.GetPagedAsync(request.Request);

            var itemsDto = pagedData.Items.Select(data => new CompanyProfileDto(
                data.Id,
                data.Name,
                data.Tagline,
                data.RegistrationNumber,
                data.Gstin,
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
                data.AuthorizedSignatories.Select(s => new AuthorizedSignatoryDto(
                    s.Id,
                    s.PersonName,
                    s.Designation,
                    s.SignatureImageUrl,
                    s.Email,
                    s.IsDefault
                )).ToList()
            )).ToList();

            return new GridResponse<CompanyProfileDto>
            {
                Items = itemsDto,
                TotalCount = pagedData.TotalCount
            };
        }
    }
}
