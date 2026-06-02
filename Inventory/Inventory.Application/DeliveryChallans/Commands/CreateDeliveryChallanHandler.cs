using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.DeliveryChallans.DTOs;
using Inventory.Domain.Entities;
using Inventory.Domain.Entities.SalesInvoice;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Inventory.Application.Clients;
using Inventory.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application.DeliveryChallans.Commands
{
    public class CreateDeliveryChallanHandler : IRequestHandler<CreateDeliveryChallanCommand, object>
    {
        private readonly IInventoryDbContext _context;
        private readonly ICustomerClient _customerClient;
        private readonly ICompanyClient _companyClient;
        private readonly IServiceScopeFactory _scopeFactory;

        public CreateDeliveryChallanHandler(
            IInventoryDbContext context,
            ICustomerClient customerClient,
            ICompanyClient companyClient,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _customerClient = customerClient;
            _companyClient = companyClient;
            _scopeFactory = scopeFactory;
        }

        public async Task<object> Handle(CreateDeliveryChallanCommand request, CancellationToken cancellationToken)
        {
            var dto = request.ChallanDto;

            // 1. Generate Challan No if null or empty
            string challanNo = dto.ChallanNo ?? string.Empty;
            if (string.IsNullOrEmpty(challanNo))
            {
                var lastChallan = await _context.DeliveryChallans
                    .OrderByDescending(x => x.CreatedOn)
                    .FirstOrDefaultAsync(cancellationToken);

                int nextId = lastChallan == null ? 1 : int.Parse(lastChallan.ChallanNo!.Split('/').Last()) + 1;
                string fyString = $"{DateTime.Now.Year}-{(DateTime.Now.Year + 1).ToString().Substring(2)}";
                challanNo = $"DC/{fyString}/{nextId:D4}";
            }

            // 2. Map DTO to Entity
            var challan = new DeliveryChallan
            {
                Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
                ChallanNo = challanNo,
                ChallanDate = dto.ChallanDate ?? DateTime.Now,
                CustomerId = dto.CustomerId,
                CustomerName = dto.CustomerName,
                SubTotal = dto.SubTotal,
                TotalTax = dto.TotalTax,
                GrandTotal = dto.GrandTotal,
                Remarks = dto.Remarks ?? "Delivery Challan",
                Status = dto.Status ?? "Pending",
                GrossWeight = dto.GrossWeight,
                VehicleRegNo = dto.VehicleRegNo,
                Origin = dto.Origin,
                Destination = dto.Destination,
                CompanyId = dto.CompanyId ?? Guid.Empty,
                BranchId = dto.BranchId,
                CreatedBy = dto.CreatedBy,
                Items = dto.Items.Select(i => new DeliveryChallanItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Qty = i.Qty,
                    Unit = i.Unit ?? "PCS",
                    Rate = i.Rate,
                    MRP = i.MRP ?? 0M,
                    DiscountPercent = i.DiscountPercent ?? 0M,
                    DiscountAmount = i.DiscountAmount ?? 0M,
                    GSTPercent = i.GstPercent ?? 0M,
                    TaxAmount = i.TaxAmount ?? 0M,
                    Total = i.Total,
                    WarehouseId = i.WarehouseId,
                    RackId = i.RackId,
                    BatchNumber = i.BatchNumber,
                    MfgDate = i.MfgDate,
                    ExpDate = i.ExpDate,
                    CompanyId = dto.CompanyId ?? Guid.Empty,
                    BranchId = dto.BranchId
                }).ToList()
            };

            await _context.DeliveryChallans.AddAsync(challan, cancellationToken);

            // 3. Deduct Stock at Challan Stage
            foreach (var item in challan.Items)
            {
                if (item.ProductId.HasValue && item.ProductId != Guid.Empty && item.Qty.HasValue)
                {
                    var transaction = new InventoryTransaction(
                        item.ProductId.Value,
                        -item.Qty.Value,
                        "DeliveryChallan",
                        challan.ChallanNo!,
                        item.WarehouseId,
                        item.RackId,
                        item.MfgDate,
                        item.ExpDate,
                        challan.CompanyId,
                        challan.BranchId,
                        null,
                        item.BatchNumber
                    );
                    await _context.InventoryTransactions.AddAsync(transaction, cancellationToken);

                    // Update WarehouseStocks
                    if (item.WarehouseId.HasValue && item.WarehouseId != Guid.Empty)
                    {
                        var whStock = await _context.WarehouseStocks
                            .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == item.WarehouseId, cancellationToken);

                        if (whStock != null)
                        {
                            whStock.Quantity -= item.Qty.Value;
                        }
                        else
                        {
                            await _context.WarehouseStocks.AddAsync(new WarehouseStock
                            {
                                ProductId = item.ProductId.Value,
                                WarehouseId = item.WarehouseId.Value,
                                Quantity = -item.Qty.Value,
                                MinStock = 0,
                                CompanyId = challan.CompanyId ?? Guid.Empty,
                                BranchId = challan.BranchId
                            }, cancellationToken);
                        }
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"[CreateDeliveryChallanHandler] DB Update Exception: {innerMessage}");
                throw new Exception($"DB Save Error: {innerMessage}", ex);
            }

            // Trigger Email Dispatch in Background
            try
            {
                var company = await _companyClient.GetCompanyProfileAsync();
                CustomerLookupDto? customer = null;
                if (challan.CustomerId.HasValue && challan.CustomerId.Value != Guid.Empty)
                {
                    customer = await _customerClient.GetCustomerByIdAsync(challan.CustomerId.Value);
                }

                _ = Task.Run(async () =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    var pdfService = scope.ServiceProvider.GetRequiredService<IPdfService>();

                    try
                    {
                        if (company != null && customer != null)
                        {
                            byte[] pdfBytes = null;
                            try
                            {
                                string challanHtml = GenerateChallanHtml(company, customer, challan);
                                pdfBytes = pdfService.Convert(challanHtml);
                            }
                            catch (Exception pdfEx)
                            {
                                Console.WriteLine($"[CreateDeliveryChallanHandler] PDF generation failed: {pdfEx.Message}");
                            }

                            if (!string.IsNullOrEmpty(customer.Email))
                            {
                                await emailService.SendDcEmailAsync(company, customer.Email, challan.ChallanNo!, challan.GrandTotal ?? 0M, pdfBytes);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CreateDeliveryChallanHandler] Notification task failed: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateDeliveryChallanHandler] Failed to kick off notification task: {ex.Message}");
            }

            return new { Id = challan.Id, ChallanNo = challan.ChallanNo };
        }

        private static string GenerateChallanHtml(Inventory.Application.Clients.DTOs.CompanyProfileDto company, CustomerLookupDto customer, DeliveryChallan challan)
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
                .transport-table {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; }}
                .transport-table td {{ padding: 8px 12px; font-size: 13px; }}
                .items-table {{ width: 100%; border-collapse: collapse; margin-top: 15px; }}
                .items-table th {{ background-color: #2563eb; color: #ffffff; font-weight: 600; text-align: left; padding: 10px 8px; font-size: 13px; }}
                .items-table td {{ padding: 10px 8px; border-bottom: 1px solid #e2e8f0; font-size: 13px; }}
                .text-right {{ text-align: right; }}
                .total-section {{ float: right; width: 320px; margin-top: 20px; background-color: #f8fafc; padding: 15px; border: 1px solid #e2e8f0; border-radius: 6px; }}
                .total-section table {{ width: 100%; border-collapse: collapse; }}
                .total-section td {{ padding: 4px 0; font-size: 13px; }}
                .total-row {{ font-weight: bold; font-size: 16px; color: #1e293b; border-top: 2px solid #e2e8f0; padding-top: 8px; }}
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
                            DELIVERY CHALLAN
                            <div style='font-size:14px; font-weight:normal; color:#475569; margin-top:4px;'>Challan No: {challan.ChallanNo}</div>
                            <div style='font-size:14px; font-weight:normal; color:#475569;'>Date: {challan.ChallanDate?.ToShortDateString()}</div>
                        </td>
                    </tr>
                </table>

                <hr style='border: 0; border-top: 1px solid #e2e8f0; margin-bottom: 20px;' />

                <table class='info-table'>
                    <tr>
                        <td>
                            <strong style='color: #1e293b; font-size: 14px;'>Dispatched From:</strong><br/>
                            <strong>{company.Name}</strong><br/>
                            {address}<br/>
                            GSTIN: {company.Gstin}<br/>
                            Phone: {company.PrimaryPhone}<br/>
                            Email: {company.PrimaryEmail}
                        </td>
                        <td style='text-align: right;'>
                            <strong style='color: #1e293b; font-size: 14px;'>Dispatched To:</strong><br/>
                            <strong>{customer?.CustomerName}</strong><br/>
                            Phone: {customer?.Phone}<br/>
                            Email: {customer?.Email}
                        </td>
                    </tr>
                </table>

                <h3 style='color: #1e293b; font-size: 14px; margin-bottom: 8px;'>Transport & Vehicle Details</h3>
                <table class='transport-table'>
                    <tr>
                        <td><strong>Vehicle No:</strong> {challan.VehicleRegNo}</td>
                        <td><strong>Gross Weight:</strong> {challan.GrossWeight} kg</td>
                    </tr>
                    <tr>
                        <td><strong>Origin:</strong> {challan.Origin}</td>
                        <td><strong>Destination:</strong> {challan.Destination}</td>
                    </tr>
                </table>

                <table class='items-table'>
                    <thead>
                        <tr>
                            <th>Item Description</th>
                            <th>Qty</th>
                            <th>Unit</th>
                            <th class='text-right'>Rate</th>
                            <th class='text-right'>Total</th>
                        </tr>
                    </thead>
                    <tbody>");

            foreach (var item in challan.Items)
            {
                sb.Append($@"
                        <tr>
                            <td>{item.ProductName}</td>
                            <td>{item.Qty}</td>
                            <td>{item.Unit}</td>
                            <td class='text-right'>₹{item.Rate:N2}</td>
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
                            <td class='text-right'>₹{challan.SubTotal:N2}</td>
                        </tr>
                        <tr>
                            <td>Tax</td>
                            <td class='text-right'>₹{challan.TotalTax:N2}</td>
                        </tr>
                        <tr class='total-row'>
                            <td style='padding-top: 8px;'>Grand Total</td>
                            <td class='text-right' style='padding-top: 8px;'>₹{challan.GrandTotal:N2}</td>
                        </tr>
                    </table>
                </div>
                <div style='clear: both;'></div>

                <div class='footer'>
                    Thank you for your business! If you have any questions, please contact us at {company.PrimaryEmail}.<br/>
                    <em>Regd. office: {address}</em>
                </div>
            </div>
        </body>
        </html>");

            return sb.ToString();
        }
    }
}
