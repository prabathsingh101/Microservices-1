using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Clients;
using Inventory.Application.Clients.DTOs;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.PurchaseReturn;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record GetRfqPdfQuery(Guid Id) : IRequest<byte[]?>;

public class GetRfqPdfQueryHandler : IRequestHandler<GetRfqPdfQuery, byte[]?>
{
    private readonly IInventoryDbContext _context;
    private readonly ICompanyClient _companyClient;
    private readonly ISupplierClient _supplierClient;
    private readonly IPdfService _pdfService;

    public GetRfqPdfQueryHandler(
        IInventoryDbContext context,
        ICompanyClient companyClient,
        ISupplierClient supplierClient,
        IPdfService pdfService)
    {
        _context = context;
        _companyClient = companyClient;
        _supplierClient = supplierClient;
        _pdfService = pdfService;
    }

    public async Task<byte[]?> Handle(GetRfqPdfQuery request, CancellationToken ct)
    {
        var rfq = await _context.RequestForQuotations
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (rfq == null) return null;

        var company = await _companyClient.GetCompanyProfileAsync();
        var supplier = await _supplierClient.GetSupplierByIdAsync(rfq.SupplierId);

        if (company == null) return null;

        string rfqHtml = GenerateRfqHtml(company, supplier, rfq);
        return _pdfService.Convert(rfqHtml);
    }

    private static string GenerateRfqHtml(CompanyProfileDto company, SupplierSelectDto supplier, RequestForQuotation rfq)
    {
        var sb = new StringBuilder();
        var address = company.Address != null 
            ? $"{company.Address.AddressLine1}, {company.Address.City}, {company.Address.State} - {company.Address.PinCode}" 
            : "";

        sb.Append($@"
        <html>
        <head>
            <style>
                body {{ font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; padding: 15px; color: #333; }}
                .invoice-box {{ max-width: 800px; margin: auto; padding: 10px; font-size: 14px; line-height: 24px; }}
                .header-table {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; }}
                .header-table td {{ vertical-align: top; }}
                .logo-container {{ font-size: 28px; font-weight: bold; color: #2563eb; }}
                .invoice-title {{ font-size: 24px; font-weight: bold; text-align: right; color: #1e293b; }}
                .info-table {{ width: 100%; border-collapse: collapse; margin-bottom: 25px; }}
                .info-table td {{ width: 50%; vertical-align: top; font-size: 13px; }}
                .items-table {{ width: 100%; border-collapse: collapse; margin-top: 15px; }}
                .items-table th {{ background-color: #2563eb; color: #ffffff; font-weight: 600; text-align: left; padding: 10px 8px; font-size: 13px; }}
                .items-table td {{ padding: 10px 8px; border-bottom: 1px solid #e2e8f0; font-size: 13px; }}
                .text-right {{ text-align: right; }}
                .footer {{ text-align: center; margin-top: 100px; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0; padding-top: 15px; }}
            </style>
        </head>
        <body>
            <div class='invoice-box'>
                <table class='header-table'>
                    <tr>
                        <td class='logo-container'>
                            {company.Name}
                            <div style='font-size:12px; font-weight:normal; color:#64748b; margin-top:2px;'>{company.Tagline}</div>
                        </td>
                        <td class='invoice-title'>
                            REQUEST FOR QUOTATION
                            <div style='font-size:14px; font-weight:normal; color:#475569; margin-top:4px;'>RFQ No: {rfq.RfqNo}</div>
                            <div style='font-size:14px; font-weight:normal; color:#475569;'>Date: {rfq.CreatedDate.ToShortDateString()}</div>
                            {(rfq.ExpiryDate.HasValue ? $"<div style='font-size:14px; font-weight:normal; color:#475569;'>Expiry Date: {rfq.ExpiryDate.Value.ToShortDateString()}</div>" : "")}
                        </td>
                    </tr>
                </table>

                <hr style='border: 0; border-top: 1px solid #e2e8f0; margin-bottom: 20px;' />

                <table class='info-table'>
                    <tr>
                        <td>
                            <strong style='color: #1e293b; font-size: 14px;'>From:</strong><br/>
                            <strong>{company.Name}</strong><br/>
                            {address}<br/>
                            GSTIN: {company.Gstin}<br/>
                            Phone: {company.PrimaryPhone}<br/>
                            Email: {company.PrimaryEmail}
                        </td>
                        <td style='text-align: right;'>
                            <strong style='color: #1e293b; font-size: 14px;'>To Supplier:</strong><br/>
                            <strong>{supplier?.Name}</strong><br/>
                            Phone: {supplier?.Phone}<br/>
                            Email: {supplier?.Email}
                        </td>
                    </tr>
                </table>

                {(string.IsNullOrEmpty(rfq.Remarks) ? "" : $"<div style='margin-bottom: 20px;'><strong>Remarks:</strong> {rfq.Remarks}</div>")}

                <table class='items-table'>
                    <thead>
                        <tr>
                            <th style='width: 10%;'>S.No</th>
                            <th style='width: 70%;'>Product Name</th>
                            <th style='width: 20%;' class='text-right'>Quantity</th>
                        </tr>
                    </thead>
                    <tbody>");

        int index = 1;
        foreach (var item in rfq.Items)
        {
            var productName = item.Product?.Name ?? "Unknown Product";
            sb.Append($@"
                        <tr>
                            <td>{index++}</td>
                            <td>{productName}</td>
                            <td class='text-right'>{item.Qty}</td>
                        </tr>");
        }

        sb.Append($@"
                    </tbody>
                </table>

                <div class='footer'>
                    This is a system generated Request for Quotation document.
                </div>
            </div>
        </body>
        </html>");

        return sb.ToString();
    }
}
