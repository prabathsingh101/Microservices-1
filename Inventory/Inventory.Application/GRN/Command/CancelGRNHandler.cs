using Inventory.Application.Clients;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.GRN.Command
{
    public class CancelGRNHandler : IRequestHandler<CancelGRNCommand, bool>
    {
        private readonly IGRNRepository _repo;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ICompanyClient _companyClient;
        private readonly ISupplierClient _supplierClient;
        private readonly IInventoryDbContext _context;

        public CancelGRNHandler(
            IGRNRepository repo,
            IServiceScopeFactory scopeFactory,
            ICompanyClient companyClient,
            ISupplierClient supplierClient,
            IInventoryDbContext context)
        {
            _repo = repo;
            _scopeFactory = scopeFactory;
            _companyClient = companyClient;
            _supplierClient = supplierClient;
            _context = context;
        }

        public async Task<bool> Handle(CancelGRNCommand request, CancellationToken ct)
        {
            // We will fetch the GRN using the existing print method or a new one
            var grnHeader = await _repo.GetGrnBasicDetailsAsync(request.GrnId);
            if (grnHeader == null)
            {
                throw new Exception("GRN not found");
            }

            // Validation: Check if there is an active Purchase Return linked to this GRN
            var hasActiveReturn = await _context.PurchaseReturnItems
                .AnyAsync(ri => ri.GrnRef == grnHeader.GRNNumber && 
                                ri.PurchaseReturn.Status != "Cancelled" && 
                                ri.PurchaseReturn.Status != "Canceled", 
                          ct);

            if (hasActiveReturn)
            {
                throw new Exception("An active Purchase Return exists for this GRN, so it cannot be cancelled. Please cancel the Purchase Return first.");
            }

            // Reverse stock
            bool stockReversed = await _repo.CancelGRNWithStockReversal(request.GrnId);

            if (stockReversed && grnHeader.SupplierId != Guid.Empty && grnHeader.TotalAmount > 0)
            {
                // Reverse Ledger (Supplier Credit Reversal)
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var supplierClient = scope.ServiceProvider.GetRequiredService<ISupplierClient>();
                    
                    string description = $"Cancellation Reversal for GRN: {grnHeader.GRNNumber}";
                    if (!string.IsNullOrWhiteSpace(request.Reason))
                    {
                        description += $" | Reason: {request.Reason}";
                    }
                    // RecordPurchaseReturnAsync can be used to reverse the ledger
                    await supplierClient.RecordPurchaseReturnAsync(grnHeader.SupplierId, grnHeader.TotalAmount, grnHeader.GRNNumber ?? "N/A", description, request.CancelledBy);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CancelGRNHandler] WARNING: Financial reversal failed: {ex.Message}");
                }

                // 🔔 EMAIL NOTIFICATION & PDF TRIGGER IN BACKGROUND
                try
                {
                    // Fetch data on request thread where HTTP context / JWT headers are active
                    var company = await _companyClient.GetCompanyProfileAsync();
                    var supplier = await _supplierClient.GetSupplierByIdAsync(grnHeader.SupplierId);

                    if (company != null && supplier != null && !string.IsNullOrEmpty(supplier.Email))
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using var scope = _scopeFactory.CreateScope();
                                var pdfService = scope.ServiceProvider.GetRequiredService<IPdfService>();
                                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                                var scopedRepo = scope.ServiceProvider.GetRequiredService<IGRNRepository>();

                                // Fetch detailed items for print DTO using scoped repository to prevent ObjectDisposedException
                                var grnDetails = await scopedRepo.GetGrnDetailsByNumberAsync(grnHeader.GRNNumber, grnHeader.CompanyId);

                                byte[] pdfBytes = null;
                                try
                                {
                                    string grnHtml = GenerateCancelledGrnHtml(company, supplier, grnHeader, grnDetails, request.Reason ?? "Not Specified");
                                    pdfBytes = pdfService.Convert(grnHtml);
                                }
                                catch (Exception pdfEx)
                                {
                                    Console.WriteLine($"[CancelGRNHandler] PDF generation failed: {pdfEx.Message}");
                                }

                                await emailService.SendCancelledGrnEmailAsync(
                                    company, 
                                    supplier.Email, 
                                    grnHeader.GRNNumber ?? "N/A", 
                                    grnDetails?.PoNumber ?? "N/A", 
                                    grnHeader.TotalAmount, 
                                    request.Reason ?? "Not Specified", 
                                    pdfBytes
                                );
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[CancelGRNHandler] Background notification task failed: {ex.Message}");
                            }
                        });
                    }
                }
                catch (Exception threadFetchEx)
                {
                    Console.WriteLine($"[CancelGRNHandler] Data fetch for background task failed: {threadFetchEx.Message}");
                }
            }

            return stockReversed;
        }

        private static string GenerateCancelledGrnHtml(
            Inventory.Application.Clients.DTOs.CompanyProfileDto company, 
            Inventory.Application.PurchaseReturn.SupplierSelectDto supplier, 
            Domain.Entities.GRNHeader grnHeader, 
            GrnPrintDto grnDetails, 
            string reason)
        {
            var sb = new StringBuilder();
            var companyAddress = company.Address != null 
                ? $"{company.Address.AddressLine1}, {company.Address.City}, {company.Address.State} - {company.Address.PinCode}" 
                : "";

            sb.Append($@"
    <html>
    <head>
        <style>
            body {{ font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; padding: 15px; color: #333; position: relative; }}
            .cancelled-watermark {{
                position: absolute;
                top: 40%;
                left: 5%;
                width: 90%;
                text-align: center;
                font-size: 80px;
                color: rgba(220, 38, 38, 0.15);
                font-weight: bold;
                transform: rotate(-30deg);
                pointer-events: none;
                z-index: 1000;
                text-transform: uppercase;
                border: 15px solid rgba(220, 38, 38, 0.15);
                padding: 10px;
                border-radius: 20px;
            }}
            .invoice-box {{ max-width: 800px; margin: auto; padding: 10px; font-size: 14px; line-height: 24px; }}
            .header-table {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; }}
            .header-table td {{ vertical-align: top; }}
            .logo-container {{ font-size: 28px; font-weight: bold; color: #dc2626; }}
            .invoice-title {{ font-size: 22px; font-weight: bold; text-align: right; color: #dc2626; }}
            .info-table {{ width: 100%; border-collapse: collapse; margin-bottom: 25px; }}
            .info-table td {{ width: 50%; vertical-align: top; font-size: 13px; }}
            .items-table {{ width: 100%; border-collapse: collapse; margin-top: 15px; }}
            .items-table th {{ background-color: #dc2626; color: #ffffff; font-weight: 600; text-align: left; padding: 10px 8px; font-size: 13px; }}
            .items-table td {{ padding: 10px 8px; border-bottom: 1px solid #e2e8f0; font-size: 13px; }}
            .text-right {{ text-align: right; }}
            .total-section {{ float: right; width: 320px; margin-top: 20px; background-color: #f8fafc; padding: 15px; border: 1px solid #e2e8f0; border-radius: 6px; }}
            .total-section table {{ width: 100%; border-collapse: collapse; }}
            .total-section td {{ padding: 4px 0; font-size: 13px; }}
            .total-row {{ font-weight: bold; font-size: 16px; color: #dc2626; border-top: 2px solid #e2e8f0; padding-top: 8px; }}
            .cancellation-banner {{ background-color: #fef2f2; border-left: 6px solid #dc2626; padding: 15px; margin-bottom: 25px; border-radius: 4px; }}
            .cancellation-banner h3 {{ margin: 0 0 5px 0; color: #991b1b; font-size: 16px; }}
            .cancellation-banner p {{ margin: 0; color: #7f1d1d; font-size: 13px; }}
            .footer {{ text-align: center; margin-top: 80px; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0; padding-top: 15px; }}
        </style>
    </head>
    <body>
        <div class='cancelled-watermark'>CANCELLED</div>
        <div class='invoice-box'>
            <table class='header-table'>
                <tr>
                    <td class='logo-container'>
                        {company.Name}
                        <div style='font-size:12px; font-weight:normal; color:#64748b; margin-top:2px;'>{company.Tagline}</div>
                    </td>
                    <td class='invoice-title'>
                        GOODS RECEIVED ADVICE<br/>
                        <span style='font-size:18px; color:#ef4444;'>[CANCELLED]</span>
                        <div style='font-size:13px; font-weight:normal; color:#475569; margin-top:4px;'>GRN No: {grnHeader.GRNNumber}</div>
                        <div style='font-size:13px; font-weight:normal; color:#475569;'>Date: {grnHeader.ReceivedDate.ToShortDateString()}</div>
                    </td>
                </tr>
            </table>

            <div class='cancellation-banner'>
                <h3>This transaction has been Cancelled</h3>
                <p><strong>Reason for cancellation:</strong> {(!string.IsNullOrWhiteSpace(reason) ? reason : "No reason provided")}</p>
                <p><strong>Cancelled On:</strong> {DateTime.Now.ToString("dd MMM yyyy, hh:mm tt")}</p>
            </div>

            <table class='info-table'>
                <tr>
                    <td>
                        <strong style='color: #dc2626; font-size: 14px;'>Company Info:</strong><br/>
                        <strong>{company.Name}</strong><br/>
                        {companyAddress}<br/>
                        GSTIN: {company.Gstin}<br/>
                        Phone: {company.PrimaryPhone}<br/>
                        Email: {company.PrimaryEmail}
                    </td>
                    <td style='text-align: right;'>
                        <strong style='color: #dc2626; font-size: 14px;'>Supplier Info:</strong><br/>
                        <strong>{supplier.Name}</strong><br/>
                        Phone: {supplier.Phone}<br/>
                        Email: {supplier.Email}
                    </td>
                </tr>
            </table>");

            if (grnDetails?.Items != null && grnDetails.Items.Any())
            {
                sb.Append($@"
            <table class='items-table'>
                <thead>
                    <tr>
                        <th>Item Description</th>
                        <th>Ordered Qty</th>
                        <th>Received Qty</th>
                        <th class='text-right'>Rate</th>
                        <th class='text-right'>Tax%</th>
                        <th class='text-right'>Total</th>
                    </tr>
                </thead>
                <tbody>");

                foreach (var item in grnDetails.Items)
                {
                    sb.Append($@"
                    <tr>
                        <td>{item.ProductName}</td>
                        <td>{item.OrderedQty}</td>
                        <td>{item.ReceivedQty}</td>
                        <td class='text-right'>₹{item.UnitRate:N2}</td>
                        <td class='text-right'>{item.GstPercentage}%</td>
                        <td class='text-right'>₹{item.Total:N2}</td>
                    </tr>");
                }

                sb.Append($@"
                </tbody>
            </table>

            <div class='total-section'>
                <table>
                    <tr>
                        <td>Subtotal</td>
                        <td class='text-right'>₹{grnDetails.SubTotal:N2}</td>
                    </tr>
                    <tr>
                        <td>Tax Amount</td>
                        <td class='text-right'>₹{grnDetails.TotalTaxAmount:N2}</td>
                    </tr>
                    <tr class='total-row'>
                        <td style='padding-top: 8px;'>Grand Total</td>
                        <td class='text-right' style='padding-top: 8px;'>₹{grnDetails.TotalAmount:N2}</td>
                    </tr>
                </table>
            </div>
            <div style='clear: both;'></div>");
            }
            else
            {
                sb.Append($@"
            <div style='text-align: center; padding: 30px; border: 1px solid #e2e8f0; color: #64748b;'>
                No items found for this Goods Received Advice.
            </div>");
            }

            sb.Append($@"
            <div class='footer'>
                This is a system generated cancellation notification document.
            </div>
        </div>
    </body>
    </html>");

            return sb.ToString();
        }
    }
}
