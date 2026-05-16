using System.Text;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.SaleOrders.Commands;
using Inventory.Application.Clients;
using Inventory.Application.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Inventory.Domain.Entities.SO;
using Inventory.Domain.Entities;

public class CreateSaleOrderHandler : IRequestHandler<CreateSaleOrderCommand, object>
{
    private readonly ISaleOrderRepository _repo;
    private readonly IInventoryDbContext _context;
    private readonly ICustomerClient _customerClient;
    private readonly IServiceScopeFactory _scopeFactory;

    public CreateSaleOrderHandler(
        ISaleOrderRepository repo, 
        IInventoryDbContext context, 
        IServiceScopeFactory scopeFactory,
        ICustomerClient customerClient)
    {
        _repo = repo;
        _context = context;
        _scopeFactory = scopeFactory;
        _customerClient = customerClient;
    }

    public async Task<object> Handle(CreateSaleOrderCommand request, CancellationToken cancellationToken)
    {
        var dto = request.OrderDto;
        bool isEdit = dto.Id != Guid.Empty;
        string? existingSONo = null;
        string? oldStatus = null;

        if (isEdit)
        {
            var existing = await _context.SaleOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.Id);
            if (existing != null)
            {
                existingSONo = existing.SONumber;
                oldStatus = existing.Status;
            }
        }

        // 1. SONumber Setup
        string finalSONo = existingSONo;
        if (string.IsNullOrEmpty(finalSONo))
        {
            string lastNo = await _repo.GetLastSONumberAsync();
            int nextId = lastNo == null ? 1 : int.Parse(lastNo.Split('-').Last()) + 1;
            
            string prefix = dto.IsQuick ? "SO-Q" : "SO";
            finalSONo = $"{prefix}-{DateTime.Now.Year}-{nextId:D4}";
        }

        // 🚀 AUTO-RESOLVE BRANCH ID FROM WAREHOUSE IF MISSING
        if (string.IsNullOrEmpty(dto.BranchId) && dto.Items != null && dto.Items.Any())
        {
            var firstWhId = dto.Items.FirstOrDefault(i => i.WarehouseId != null)?.WarehouseId;
            if (firstWhId != null)
            {
                var warehouse = await _context.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == firstWhId);
                if (warehouse != null && !string.IsNullOrEmpty(warehouse.BranchId))
                {
                    dto.BranchId = warehouse.BranchId;
                }
            }
        }

        // 2. SaleOrder Object Mapping
        var saleOrder = new SaleOrder
        {
            Id = dto.Id,
            CompanyId = dto.CompanyId,
            BranchId = dto.BranchId,
            SONumber = finalSONo,
            CustomerId = dto.CustomerId,
            SODate = dto.SoDate,
            ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
            SubTotal = dto.SubTotal,
            TotalTax = dto.TotalTax,
            GrandTotal = dto.GrandTotal,
            TaxType = dto.TaxType,
            TdsPercent = dto.TdsPercent,
            TdsAmount = dto.TdsAmount,
            TcsPercent = dto.TcsPercent,
            TcsAmount = dto.TcsAmount,
            IgstAmount = dto.IgstAmount,
            CgstAmount = dto.CgstAmount,
            SgstAmount = dto.SgstAmount,
            Remarks = dto.Remarks,
            Status = dto.Status,
            CreatedBy = dto.CreatedBy,
            IsQuick = dto.IsQuick, // Map flag from DTO
            GuestName = dto.GuestName,
            GuestPhone = dto.GuestPhone,
            Items = dto.Items.Select(i => new SaleOrderItem
            {
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
                MfgDate = i.ManufacturingDate,
                ExpDate = i.ExpiryDate,
                WarehouseId = i.WarehouseId,
                RackId = i.RackId,
                BatchNumber = i.BatchNumber,
                ReferenceNumber = i.ReferenceNumber,
                CompanyId = dto.CompanyId,
                BranchId = dto.BranchId
            }).ToList()
        };

        bool shouldProcessConfirmed = (dto.Status == "Confirmed");
        object? result = null;

        if (shouldProcessConfirmed)
        {
            await _repo.ExecuteInTransactionAsync(async () =>
            {
                decimal oldGrandTotal = 0;
                if (isEdit && oldStatus == "Confirmed")
                {
                    // 1. Revert Old Stock and Old Ledger
                    var existingWithItems = await _context.SaleOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == dto.Id);
                    if (existingWithItems != null)
                    {
                        oldGrandTotal = existingWithItems.GrandTotal;
                        foreach (var item in existingWithItems.Items)
                        {
                            // ⚡ REDUNDANT: Products.CurrentStock update removed.

                            // 🆕 Record Reversal in Audit Trail
                            var reversalTx = new InventoryTransaction(
                                item.ProductId,
                                item.Qty, // Positive because it is READDING stock
                                (existingWithItems.IsQuick ? "QuickSale" : "Sale") + "-REVERSAL",
                                existingWithItems.SONumber,
                                item.WarehouseId,
                                item.RackId,
                                item.MfgDate,
                                item.ExpDate,
                                existingWithItems.CompanyId,
                                existingWithItems.BranchId,
                                item.ReferenceNumber,
                                item.BatchNumber
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

                        // Optional: Record reversal for OLD amount before recording NEW
                        if (existingWithItems.CustomerId.HasValue && existingWithItems.CustomerId.Value != Guid.Empty)
                        {
                            try
                            {
                                await _customerClient.RecordSaleAsync(
                                    existingWithItems.CustomerId.Value,
                                    -existingWithItems.GrandTotal,
                                    existingWithItems.SONumber,
                                    $"Sale Order Adjustment (Old Reversal): {existingWithItems.SONumber}",
                                    "System",
                                    Guid.TryParse(existingWithItems.BranchId, out var branchId) ? branchId : (Guid?)null,
                                    existingWithItems.CompanyId
                                );
                            }
                            catch (Exception ex) { Console.WriteLine($"Old Ledger reversal failed: {ex.Message}"); }
                        }
                    }
                }

                Guid savedId;
                if (isEdit)
                {
                    await _repo.UpdateAsync(saleOrder);
                    savedId = saleOrder.Id;
                }
                else
                {
                    savedId = await _repo.SaveAsync(saleOrder);
                }

                // 2. Deduct New Stock
                foreach (var item in saleOrder.Items)
                {
                    decimal availableStock = await _repo.GetAvailableStockAsync(item.ProductId);
                    Console.WriteLine($"[StockCheck] Product: {item.ProductName}, Required: {item.Qty}, Available: {availableStock}");
                    if (availableStock < item.Qty)
                    {
                        throw new Exception($"Insufficient stock for {item.ProductName}. Available: {availableStock}");
                    }

                    // 🆕 Record Inventory Transaction
                    var saleTx = new InventoryTransaction(
                        item.ProductId,
                        -item.Qty, // Negative because it is REDUCING stock
                        saleOrder.IsQuick ? "QuickSale" : "Sale",
                        saleOrder.SONumber,
                        item.WarehouseId,
                        item.RackId,
                        item.MfgDate,
                        item.ExpDate,
                        saleOrder.CompanyId,
                        saleOrder.BranchId,
                        item.ReferenceNumber,
                        item.BatchNumber
                    );
                    await _context.InventoryTransactions.AddAsync(saleTx);

                    // 🚀 UPDATE PHYSICAL WAREHOUSE STOCK
                    if (item.WarehouseId.HasValue && item.WarehouseId != Guid.Empty)
                    {
                        var whStock = await _context.WarehouseStocks
                            .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == item.WarehouseId);

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
                                CompanyId = saleOrder.CompanyId,
                                BranchId = saleOrder.BranchId
                            });
                        }
                    }
                }

                // ⚡ CRITICAL FIX: Persist WarehouseStock and Transaction changes to DB
                await _context.SaveChangesAsync();

                // 3. Record New Ledger
                if (saleOrder.CustomerId.HasValue && saleOrder.CustomerId.Value != Guid.Empty)
                {
                    try
                    {
                        await _customerClient.RecordSaleAsync(
                            saleOrder.CustomerId.Value,
                            saleOrder.GrandTotal,
                            saleOrder.SONumber,
                            $"Sale Invoice generated: {saleOrder.SONumber}",
                            saleOrder.CreatedBy ?? "System",
                            Guid.TryParse(saleOrder.BranchId, out var branchId) ? branchId : (Guid?)null,
                            saleOrder.CompanyId
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ledger sync failed: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"[CreateSaleOrderHandler] Skipping ledger sync for Walking Customer: {saleOrder.GuestName}");
                }

                result = new { Id = savedId, SONumber = finalSONo };
            });
        }
        else
        {
            // Simple Save/Update without stock deduction
            if (isEdit)
            {
                await _repo.UpdateAsync(saleOrder);
                result = new { Id = saleOrder.Id, SONumber = finalSONo };
            }
            else
            {
                var savedId = await _repo.SaveAsync(saleOrder);
                result = new { Id = savedId, SONumber = finalSONo };
            }
        }

        // 4. Notifications
        if (result != null && dto.Status == "Confirmed" && (oldStatus == null || oldStatus != "Confirmed"))
        {
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var companyClient = scope.ServiceProvider.GetRequiredService<ICompanyClient>();
                var customerClient = scope.ServiceProvider.GetRequiredService<ICustomerClient>();
                var pdfService = scope.ServiceProvider.GetRequiredService<IPdfService>();

                try
                {
                    var company = await companyClient.GetCompanyProfileAsync();
                    
                    CustomerLookupDto? customer = null;
                    if (saleOrder.CustomerId.HasValue && saleOrder.CustomerId.Value != Guid.Empty)
                    {
                        customer = await customerClient.GetCustomerByIdAsync(saleOrder.CustomerId.Value);
                    }
                    else if (!string.IsNullOrEmpty(saleOrder.GuestName))
                    {
                        customer = new CustomerLookupDto 
                        { 
                            CustomerName = saleOrder.GuestName,
                            Phone = saleOrder.GuestPhone,
                            Email = "" // Guest typically doesn't have email in this simplified flow
                        };
                    }

                    if (company != null && customer != null)
                    {
                        byte[] pdfBytes = null;
                        try
                        {
                            string invoiceHtml = GenerateInvoiceHtml(company, customer, saleOrder);
                            pdfBytes = pdfService.Convert(invoiceHtml);
                        }
                        catch (Exception pdfEx)
                        {
                            Console.WriteLine($"[CreateSaleOrderHandler] PDF generation failed: {pdfEx.Message}");
                        }

                        if (!string.IsNullOrEmpty(customer.Email))
                        {
                            await emailService.SendSoEmailAsync(company, customer.Email, finalSONo, saleOrder.GrandTotal, pdfBytes);
                        }
                        if (!string.IsNullOrEmpty(customer.Phone))
                        {
                            string msg = $"Order Confirmed! 🚀\nFrom: {company.Name}\nOrder No: {finalSONo}\nAmount: {saleOrder.GrandTotal}\nThank you for shopping with us!";
                            await whatsAppService.SendMessageAsync(customer.Phone, msg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CreateSaleOrderHandler] Notification task failed: {ex.Message}");
                }
            });
        }

        return result;
    }

    private static string GenerateInvoiceHtml(Inventory.Application.Clients.DTOs.CompanyProfileDto company, CustomerLookupDto customer, SaleOrder saleOrder)
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
                        <div style='font-size:14px; font-weight:normal; color:#475569; margin-top:4px;'>Invoice No: {saleOrder.SONumber}</div>
                        <div style='font-size:14px; font-weight:normal; color:#475569;'>Date: {saleOrder.SODate.ToShortDateString()}</div>
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
                Thank you for shopping with us! If you have any questions, please contact us at {company.PrimaryEmail}.<br/>
                <em>Regd. office: {address}</em>
            </div>
        </div>
    </body>
    </html>");

        return sb.ToString();
    }
}
