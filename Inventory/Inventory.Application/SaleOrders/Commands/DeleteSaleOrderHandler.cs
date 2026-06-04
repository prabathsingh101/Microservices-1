using Inventory.Application.Clients;
using Inventory.Application.Clients.DTOs;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.SaleOrders.Commands
{
    public class DeleteSaleOrderHandler : IRequestHandler<DeleteSaleOrderCommand, bool>
    {
        private readonly ISaleOrderRepository _repo;
        private readonly ICustomerClient _customerClient;
        private readonly ICompanyClient _companyClient;
        private readonly IInventoryDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;

        public DeleteSaleOrderHandler(
            ISaleOrderRepository repo, 
            ICustomerClient customerClient, 
            ICompanyClient companyClient,
            IInventoryDbContext context,
            IServiceScopeFactory scopeFactory)
        {
            _repo = repo;
            _customerClient = customerClient;
            _companyClient = companyClient;
            _context = context;
            _scopeFactory = scopeFactory;
        }

        public async Task<bool> Handle(DeleteSaleOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _repo.GetSaleOrderByIdAsync(request.Id);
            if (order == null) return false;

            bool deleted = false;

            await _repo.ExecuteInTransactionAsync(async () =>
            {
                // 1. If Order was Confirmed/Delivered/Completed, Revert Stock
                if (order.Status == "Confirmed" || order.Status == "Delivered" || order.Status == "Completed")
                {
                    foreach (var item in order.Items)
                    {
                        // 🆕 Record Reversal in Audit Trail
                        var reversalTx = new InventoryTransaction(
                            item.ProductId,
                            item.Qty, // Positive because it is READDING stock
                            (order.IsQuick ? "QuickSale" : "Sale") + "-DELETED",
                            order.SoNumber,
                            item.WarehouseId,
                            item.RackId,
                            item.ManufacturingDate,
                            item.ExpiryDate,
                            order.CompanyId,
                            order.BranchId
                        );
                        await _context.InventoryTransactions.AddAsync(reversalTx);

                        // 🚀 RESTORE PHYSICAL WAREHOUSE STOCK
                        if (item.WarehouseId.HasValue && item.WarehouseId != Guid.Empty)
                        {
                            var whStock = await _context.WarehouseStocks
                                .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == item.WarehouseId);

                            if (whStock != null)
                            {
                                whStock.Quantity += item.Qty;
                            }
                        }
                    }

                    // 2. Ledger Sync (Reverse Sale)
                    if (order.CustomerId.HasValue && order.CustomerId.Value != Guid.Empty)
                    {
                        try
                        {
                            string ledgerNote = $"Sale Order Deleted/Cancelled: {order.SoNumber}";
                            if (!string.IsNullOrWhiteSpace(request.Reason))
                            {
                                ledgerNote += $" | Reason: {request.Reason}";
                            }

                            // Recording a negative sale to offset the original entry
                            await _customerClient.RecordSaleAsync(
                                order.CustomerId.Value,
                                -order.GrandTotal, // Negative amount
                                order.SoNumber,
                                ledgerNote,
                                "System",
                                order.BranchId,
                                order.CompanyId
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ledger reversion failed: {ex.Message}");
                            // Note: Microservice failures are often logged but might not rollback the whole DB transaction 
                            // depending on business requirements. Here we continue to delete.
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[DeleteSaleOrder] Skipping ledger sync for Walking Customer: {order.GuestName}");
                    }
                }

                // 3. Soft Delete: Update Order Entity to Canceled
                var orderEntity = await _context.SaleOrders.FirstOrDefaultAsync(o => o.Id == request.Id);
                if (orderEntity != null)
                {
                    orderEntity.Status = "Cancelled";
                    orderEntity.CancelReason = request.Reason;
                    _context.SaleOrders.Update(orderEntity);
                    await _context.SaveChangesAsync(cancellationToken);
                    deleted = true;
                }
            });

            if (deleted)
            {
                // 🔔 EMAIL NOTIFICATION & PDF TRIGGER IN BACKGROUND
                try
                {
                    var company = await _companyClient.GetCompanyProfileAsync();
                    CustomerLookupDto? customer = null;
                    if (order.CustomerId.HasValue && order.CustomerId.Value != Guid.Empty)
                    {
                        customer = await _customerClient.GetCustomerByIdAsync(order.CustomerId.Value);
                    }
                    else if (!string.IsNullOrEmpty(order.GuestName))
                    {
                        customer = new CustomerLookupDto 
                        { 
                            CustomerName = order.GuestName,
                            Phone = order.GuestPhone,
                            Email = ""
                        };
                    }

                    if (company != null && customer != null && !string.IsNullOrEmpty(customer.Email))
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using var scope = _scopeFactory.CreateScope();
                                var pdfService = scope.ServiceProvider.GetRequiredService<IPdfService>();
                                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                                var scopedContext = scope.ServiceProvider.GetRequiredService<IInventoryDbContext>();

                                // Fetch detailed order with items inside scoped context to prevent ObjectDisposedException
                                var detailedOrder = await scopedContext.SaleOrders
                                    .IgnoreQueryFilters()
                                    .Include(so => so.Items)
                                    .FirstOrDefaultAsync(so => so.Id == order.Id);

                                if (detailedOrder == null)
                                {
                                    Console.WriteLine($"[DeleteSaleOrderHandler] Scoped SaleOrder not found for ID: {order.Id}");
                                    return;
                                }

                                byte[]? pdfBytes = null;
                                try
                                {
                                    string orderHtml = GenerateCancelledSaleOrderHtml(company, customer, detailedOrder, request.Reason ?? "Not Specified");
                                    pdfBytes = pdfService.Convert(orderHtml);
                                }
                                catch (Exception pdfEx)
                                {
                                    Console.WriteLine($"[DeleteSaleOrderHandler] PDF generation failed: {pdfEx.Message}");
                                }

                                await emailService.SendCancelledSaleOrderEmailAsync(
                                    company, 
                                    customer.Email, 
                                    detailedOrder.SONumber ?? "N/A", 
                                    detailedOrder.GrandTotal, 
                                    request.Reason ?? "Not Specified", 
                                    pdfBytes
                                );
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[DeleteSaleOrderHandler] Background notification task failed: {ex.Message}");
                            }
                        });
                    }
                }
                catch (Exception threadFetchEx)
                {
                    Console.WriteLine($"[DeleteSaleOrderHandler] Data fetch for background task failed: {threadFetchEx.Message}");
                }
            }

            return deleted;
        }

        private static string GenerateCancelledSaleOrderHtml(
            Inventory.Application.Clients.DTOs.CompanyProfileDto company, 
            CustomerLookupDto customer, 
            Domain.Entities.SO.SaleOrder saleOrder, 
            string reason)
        {
            var sb = new StringBuilder();
            var address = company.Address != null 
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
            .invoice-title {{ font-size: 24px; font-weight: bold; text-align: right; color: #dc2626; }}
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
                        SALE ORDER<br/>
                        <span style='font-size:18px; color:#ef4444;'>[CANCELLED]</span>
                        <div style='font-size:14px; font-weight:normal; color:#475569; margin-top:4px;'>Order No: {saleOrder.SONumber}</div>
                        <div style='font-size:14px; font-weight:normal; color:#475569;'>Date: {saleOrder.SODate.ToShortDateString()}</div>
                    </td>
                </tr>
            </table>

            <div class='cancellation-banner'>
                <h3>This Sale Order has been Cancelled</h3>
                <p><strong>Reason for cancellation:</strong> {(!string.IsNullOrWhiteSpace(reason) ? reason : "No reason provided")}</p>
                <p><strong>Cancelled On:</strong> {DateTime.Now.ToString("dd MMM yyyy, hh:mm tt")}</p>
            </div>

            <table class='info-table'>
                <tr>
                    <td>
                        <strong style='color: #dc2626; font-size: 14px;'>Billed From:</strong><br/>
                        <strong>{company.Name}</strong><br/>
                        {address}<br/>
                        GSTIN: {company.Gstin}<br/>
                        Phone: {company.PrimaryPhone}<br/>
                        Email: {company.PrimaryEmail}
                    </td>
                    <td style='text-align: right;'>
                        <strong style='color: #dc2626; font-size: 14px;'>Billed To:</strong><br/>
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
                        <th class='text-right'>Rate</th>
                        <th class='text-right'>Tax%</th>
                        <th class='text-right'>Total</th>
                    </tr>
                </thead>
                <tbody>");

            foreach (var item in saleOrder.Items)
            {
                sb.Append($@"
                    <tr>
                        <td>{item.ProductName}</td>
                        <td>{item.Qty}</td>
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
                        <td class='text-right'>₹{saleOrder.SubTotal:N2}</td>
                    </tr>
                    <tr>
                        <td>Tax</td>
                        <td class='text-right'>₹{saleOrder.TotalTax:N2}</td>
                    </tr>
                    <tr class='total-row'>
                        <td style='padding-top: 8px;'>Grand Total</td>
                        <td class='text-right' style='padding-top: 8px;'>₹{saleOrder.GrandTotal:N2}</td>
                    </tr>
                </table>
            </div>
            <div style='clear: both;'></div>

            <div class='footer'>
                This is a system generated cancellation notification document. If you have any questions, please contact us at {company.PrimaryEmail}.
            </div>
        </div>
    </body>
    </html>");

            return sb.ToString();
        }
    }
}
