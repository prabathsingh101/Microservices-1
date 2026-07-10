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
                data.CompanyCode,
                data.CompanyType,
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
                data.ShowBatchAndExpiry,
                data.ShowExpiredColumn,
                data.InvoiceFooterMessage,
                data.EstimateFooterMessage,
                data.PurchaseOrderFooterMessage,
                data.SaleOrderFooterMessage,
                data.PurchaseOrderCreationMessage,
                data.PurchaseOrderStatusUpdateMessage,
                data.SaleOrderCreationMessage,
                data.SaleOrderConfirmationMessage,
                data.RazorpayKeyId,
                data.RazorpaySecretKey,
                data.RazorpayXAccountNumber,
                data.Addresses.Select(addr => new AddressDto(
                    addr.Id,
                    addr.BranchName ?? "Head Office",
                    addr.AddressLine1 ?? "",
                    addr.AddressLine2 ?? "",
                    addr.City ?? "",
                    addr.State ?? "",
                    addr.StateCode ?? "",
                    addr.PinCode ?? "",
                    addr.Country ?? "India",
                    addr.Email,
                    addr.Phone,
                    addr.ContactPerson,
                    addr.Gstin,
                    addr.IsHeadOffice,
                    addr.CompanyProfileId
                )).ToList(),
                new BankDetailDto(
                    data.BankDetails.FirstOrDefault()?.Id,
                    data.BankDetails.FirstOrDefault()?.BankName ?? "",
                    data.BankDetails.FirstOrDefault()?.BranchName ?? "",
                    data.BankDetails.FirstOrDefault()?.AccountNumber ?? "",
                    data.BankDetails.FirstOrDefault()?.IfscCode ?? "",
                    data.BankDetails.FirstOrDefault()?.AccountType ?? "Current",
                    data.BankDetails.FirstOrDefault()?.Email,
                    data.BankDetails.FirstOrDefault()?.UpiId
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
                TotalCount = pagedData.TotalCount,
                ActiveCount = pagedData.ActiveCount,
                InactiveCount = pagedData.InactiveCount
            };
        }
    }
}
