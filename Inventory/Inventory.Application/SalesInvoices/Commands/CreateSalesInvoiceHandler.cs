using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Clients;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using Inventory.Domain.Entities.SalesInvoice;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application.SalesInvoices.Commands
{
    public class CreateSalesInvoiceHandler : IRequestHandler<CreateSalesInvoiceCommand, object>
    {
        private readonly IInventoryDbContext _context;
        private readonly ICustomerClient _customerClient;
        private readonly ICompanyClient _companyClient;
        private readonly IServiceScopeFactory _scopeFactory;

        public CreateSalesInvoiceHandler(
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

        public async Task<object> Handle(CreateSalesInvoiceCommand request, CancellationToken cancellationToken)
        {
            var dto = request.InvoiceDto;

            // 1. Generate Invoice No if missing
            string invoiceNo = dto.InvoiceNo;
            if (string.IsNullOrEmpty(invoiceNo))
            {
                var lastInvoice = await _context.SalesInvoices
                    .IgnoreQueryFilters()
                    .OrderByDescending(x => x.CreatedOn)
                    .FirstOrDefaultAsync(cancellationToken);
                
                int nextId = 1;
                if (lastInvoice != null && !string.IsNullOrEmpty(lastInvoice.InvoiceNo))
                {
                    var parts = lastInvoice.InvoiceNo.Split('/');
                    if (parts.Length > 0 && int.TryParse(parts.Last(), out int parsedId))
                    {
                        nextId = parsedId + 1;
                    }
                }
                string fyString = $"{DateTime.Now.Year}-{(DateTime.Now.Year + 1).ToString().Substring(2)}";
                invoiceNo = $"INV/{fyString}/{nextId:D4}";
            }

            // Map DTO to Entity
            var invoice = new SalesInvoice
            {
                Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
                InvoiceNo = invoiceNo,
                InvoiceDate = dto.InvoiceDate,
                CustomerId = dto.CustomerId,
                SubTotal = dto.SubTotal,
                TotalTax = dto.TotalTax,
                GrandTotal = dto.GrandTotal,
                TaxType = dto.TaxType,
                IgstAmount = dto.IgstAmount ?? (dto.TaxType?.ToLower() == "interstate" ? dto.TotalTax : 0M),
                CgstAmount = dto.CgstAmount ?? (dto.TaxType?.ToLower() == "local" || string.IsNullOrEmpty(dto.TaxType) ? dto.TotalTax / 2 : 0M),
                SgstAmount = dto.SgstAmount ?? (dto.TaxType?.ToLower() == "local" || string.IsNullOrEmpty(dto.TaxType) ? dto.TotalTax / 2 : 0M),
                Remarks = dto.Remarks ?? "Tax Invoice",
                Status = dto.Status ?? "Confirmed",
                IsQuick = dto.IsQuick,
                GuestName = dto.GuestName,
                GuestPhone = dto.GuestPhone,
                CustomerGstIn = dto.CustomerGstIn,
                CustomerName = dto.CustomerName,
                PlaceOfSupply = dto.PlaceOfSupply,
                DeliveryChallanId = dto.DeliveryChallanId ?? (dto.DeliveryChallanIds != null && dto.DeliveryChallanIds.Any() ? dto.DeliveryChallanIds.First() : null),
                DoctorName = dto.DoctorName,
                DoctorRegNo = dto.DoctorRegNo,
                CompanyId = dto.CompanyId ?? Guid.Empty,
                BranchId = dto.BranchId,
                CreatedBy = dto.CreatedBy,
                Items = dto.Items.Select(i => new SalesInvoiceItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Qty = i.Qty,
                    Unit = i.Unit,
                    Rate = i.Rate,
                    MRP = i.MRP,
                    DiscountAmount = i.DiscountAmount,
                    DiscountPercent = i.DiscountPercent,
                    GSTPercent = i.GstPercent,
                    TaxAmount = i.TaxAmount,
                    Total = i.Total,
                    WarehouseId = i.WarehouseId,
                    RackId = i.RackId,
                    BatchNumber = i.BatchNumber,
                    ReferenceNumber = i.ReferenceNumber,
                    MfgDate = i.ManufacturingDate,
                    ExpDate = i.ExpiryDate,
                    CompanyId = dto.CompanyId ?? Guid.Empty,
                    BranchId = dto.BranchId
                }).ToList()
            };

            await _context.SalesInvoices.AddAsync(invoice, cancellationToken);

            // Deduct Stock only if NOT generated from a Delivery Challan (since DC already deducted stock)
            bool isChallanInvoice = (invoice.DeliveryChallanId.HasValue && invoice.DeliveryChallanId.Value != Guid.Empty) ||
                                    (dto.DeliveryChallanIds != null && dto.DeliveryChallanIds.Any());

            if (!isChallanInvoice)
            {
                foreach (var item in invoice.Items)
                {
                    var saleTx = new InventoryTransaction(
                        item.ProductId,
                        -item.Qty,
                        invoice.IsQuick ? "QuickSaleInvoice" : "SaleInvoice",
                        invoice.InvoiceNo,
                        item.WarehouseId,
                        item.RackId,
                        item.MfgDate,
                        item.ExpDate,
                        invoice.CompanyId,
                        invoice.BranchId,
                        item.ReferenceNumber,
                        item.BatchNumber
                    );
                    await _context.InventoryTransactions.AddAsync(saleTx, cancellationToken);

                    if (item.WarehouseId.HasValue && item.WarehouseId != Guid.Empty)
                    {
                        var whStock = await _context.WarehouseStocks
                            .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == item.WarehouseId, cancellationToken);

                        if (whStock != null)
                        {
                            whStock.Quantity -= item.Qty;
                        }
                        else
                        {
                            await _context.WarehouseStocks.AddAsync(new WarehouseStock
                            {
                                ProductId = item.ProductId,
                                WarehouseId = item.WarehouseId.Value,
                                Quantity = -item.Qty,
                                MinStock = 0,
                                CompanyId = invoice.CompanyId,
                                BranchId = invoice.BranchId
                            }, cancellationToken);
                        }
                    }
                }
            }
            else
            {
                var challanIds = new List<Guid>();
                if (dto.DeliveryChallanIds != null && dto.DeliveryChallanIds.Any())
                {
                    challanIds.AddRange(dto.DeliveryChallanIds);
                }
                else if (invoice.DeliveryChallanId.HasValue && invoice.DeliveryChallanId.Value != Guid.Empty)
                {
                    challanIds.Add(invoice.DeliveryChallanId.Value);
                }

                foreach (var challanId in challanIds)
                {
                    var challan = await _context.DeliveryChallans
                        .FirstOrDefaultAsync(x => x.Id == challanId, cancellationToken);
                    
                    if (challan != null)
                    {
                        challan.Status = "Invoiced";
                    }

                    // Store relation
                    var relation = new Inventory.Domain.Entities.SalesInvoice.SalesInvoiceDeliveryChallan
                    {
                        SalesInvoiceId = invoice.Id,
                        DeliveryChallanId = challanId
                    };
                    await _context.SalesInvoiceDeliveryChallans.AddAsync(relation, cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Record in Finance Ledger
            if (invoice.CustomerId.HasValue && invoice.CustomerId.Value != Guid.Empty)
            {
                try
                {
                    await _customerClient.RecordSaleAsync(
                        invoice.CustomerId.Value,
                        invoice.GrandTotal,
                        invoice.InvoiceNo,
                        $"Tax Invoice generated: {invoice.InvoiceNo}",
                        invoice.CreatedBy ?? "System",
                        invoice.BranchId,
                        invoice.CompanyId
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ledger sync failed: {ex.Message}");
                }
            }

            // Trigger Email Dispatch in Background
            try
            {
                var company = await _companyClient.GetCompanyProfileAsync();
                CustomerLookupDto? customer = null;
                if (invoice.CustomerId.HasValue && invoice.CustomerId.Value != Guid.Empty)
                {
                    customer = await _customerClient.GetCustomerByIdAsync(invoice.CustomerId.Value);
                }
                else if (!string.IsNullOrEmpty(invoice.GuestName))
                {
                    customer = new CustomerLookupDto
                    {
                        CustomerName = invoice.GuestName,
                        Phone = invoice.GuestPhone,
                        Email = ""
                    };
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
                                string invoiceHtml = GenerateInvoiceHtml(company, customer, invoice);
                                pdfBytes = pdfService.Convert(invoiceHtml);
                            }
                            catch (Exception pdfEx)
                            {
                                Console.WriteLine($"[CreateSalesInvoiceHandler] PDF generation failed: {pdfEx.Message}");
                            }

                            if (!string.IsNullOrEmpty(customer.Email))
                            {
                                await emailService.SendInvoiceEmailAsync(company, customer.Email, invoice.InvoiceNo, invoice.GrandTotal, pdfBytes);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CreateSalesInvoiceHandler] Notification task failed: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateSalesInvoiceHandler] Failed to kick off notification task: {ex.Message}");
            }

            return new { Id = invoice.Id, InvoiceNo = invoice.InvoiceNo };
        }

        private static string GenerateInvoiceHtml(Inventory.Application.Clients.DTOs.CompanyProfileDto company, CustomerLookupDto customer, SalesInvoice invoice)
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
                            TAX INVOICE
                            <div style='font-size:14px; font-weight:normal; color:#475569; margin-top:4px;'>Invoice No: {invoice.InvoiceNo}</div>
                            <div style='font-size:14px; font-weight:normal; color:#475569;'>Date: {invoice.InvoiceDate.ToShortDateString()}</div>
                        </td>
                    </tr>
                </table>

                <hr style='border: 0; border-top: 1px solid #e2e8f0; margin-bottom: 20px;' />

                <table class='info-table'>
                    <tr>
                        <td>
                            <strong style='color: #1e293b; font-size: 14px;'>Billed From:</strong><br/>
                            <strong>{company.Name}</strong><br/>
                            {address}<br/>
                            GSTIN: {company.Gstin}<br/>
                            Phone: {company.PrimaryPhone}<br/>
                            Email: {company.PrimaryEmail}
                        </td>
                        <td style='text-align: right;'>
                            <strong style='color: #1e293b; font-size: 14px;'>Billed To:</strong><br/>
                            <strong>{customer?.CustomerName}</strong><br/>
                            Phone: {customer?.Phone}<br/>
                            Email: {customer?.Email}
                        </td>
                    </tr>
                </table>

                <table class='items-table'>
                    <thead>
                        <tr>
                            <th>Item Description</th>
                            <th>Qty</th>
                            <th>Unit</th>
                            <th class='text-right'>Rate</th>
                            <th class='text-right'>Tax%</th>
                            <th class='text-right'>Total</th>
                        </tr>
                    </thead>
                    <tbody>");

            foreach (var item in invoice.Items)
            {
                sb.Append($@"
                        <tr>
                            <td>{item.ProductName}</td>
                            <td>{item.Qty}</td>
                            <td>{item.Unit}</td>
                            <td class='text-right'>₹{item.Rate:N2}</td>
                            <td class='text-right'>{item.GSTPercent}%</td>
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
                            <td class='text-right'>₹{invoice.SubTotal:N2}</td>
                        </tr>
                        <tr>
                            <td>Tax</td>
                            <td class='text-right'>₹{invoice.TotalTax:N2}</td>
                        </tr>
                        <tr class='total-row'>
                            <td style='padding-top: 8px;'>Grand Total</td>
                            <td class='text-right' style='padding-top: 8px;'>₹{invoice.GrandTotal:N2}</td>
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
