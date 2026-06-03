using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Entities.SalesInvoice;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Inventory.Application.Clients;
using Inventory.Application.Clients.DTOs;
using Inventory.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure.Repositories
{
    public class StockTransferRepository : IStockTransferRepository
    {
        private readonly InventoryDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationRepository _notificationRepository;
        private readonly ICompanyClient _companyClient;
        private readonly IServiceScopeFactory _scopeFactory;

        public StockTransferRepository(
            InventoryDbContext context, 
            ICurrentUserService currentUserService,
            INotificationRepository notificationRepository,
            ICompanyClient companyClient,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _currentUserService = currentUserService;
            _notificationRepository = notificationRepository;
            _companyClient = companyClient;
            _scopeFactory = scopeFactory;
        }

        public async Task<string> CreateTransferAsync(StockTransferHeader header, List<StockTransferDetail> details)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var companyId = _currentUserService.CompanyId ?? Guid.Empty;
                    
                    // 1. Generate Transfer Number if not provided
                    if (string.IsNullOrEmpty(header.TransferNumber))
                    {
                        var count = await _context.StockTransferHeaders.IgnoreQueryFilters().CountAsync(x => x.CompanyId == companyId);
                        header.SetTransferNumber($"TRF-{DateTime.Now.Year}-{(count + 1001)}");
                    }

                    await _context.StockTransferHeaders.AddAsync(header);
                    await _context.SaveChangesAsync();

                    // Query From and To Warehouses to get names for the Delivery Challan
                    var fromWarehouse = await _context.Warehouses.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(w => w.Id == header.FromWarehouseId && w.CompanyId == companyId);
                    var toWarehouse = await _context.Warehouses.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(w => w.Id == header.ToWarehouseId && w.CompanyId == companyId);

                    // Query Product details to compute rates and taxes for Delivery Challan Items
                    var productIds = details.Select(d => d.ProductId).Distinct().ToList();
                    var products = await _context.Products.IgnoreQueryFilters()
                        .Where(p => productIds.Contains(p.Id) && p.CompanyId == companyId)
                        .ToDictionaryAsync(p => p.Id);

                    decimal subTotal = 0M;
                    decimal totalTax = 0M;
                    var challanItems = new List<DeliveryChallanItem>();

                    foreach (var item in details)
                    {
                        item.StockTransferHeaderId = header.Id;
                        await _context.StockTransferDetails.AddAsync(item);

                        // 🚀 STOCK UPDATE LOGIC (Step 1: Deduct from Source Warehouse only)
                        
                        // A. Deduct from Source Warehouse
                        var sourceStock = await _context.WarehouseStocks
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == header.FromWarehouseId && ws.CompanyId == companyId);
                        
                        if (sourceStock == null || sourceStock.Quantity < item.Quantity)
                        {
                            throw new Exception($"Insufficient stock for Product ID {item.ProductId} in source warehouse.");
                        }
                        sourceStock.Quantity -= item.Quantity;

                        // B. Record Inventory Transactions (OUT from Source)
                        await _context.InventoryTransactions.AddAsync(new InventoryTransaction(
                            item.ProductId,
                            -item.Quantity,
                            "Transfer-Out",
                            header.TransferNumber,
                            header.FromWarehouseId,
                            null, // Rack support can be added later
                            null, null,
                            companyId,
                            header.FromBranchId,
                            null, // ReferenceNumber not applicable here (Transfer)
                            item.BatchNumber
                        ));

                        // C. Build Delivery Challan Item
                        products.TryGetValue(item.ProductId, out var product);
                        
                        decimal rate = product?.BasePurchasePrice ?? 0M;
                        decimal gstPercent = product?.DefaultGst ?? 0M;
                        decimal itemSubTotal = item.Quantity * rate;
                        decimal taxAmount = itemSubTotal * (gstPercent / 100M);
                        decimal itemTotal = itemSubTotal + taxAmount;

                        subTotal += itemSubTotal;
                        totalTax += taxAmount;

                        challanItems.Add(new DeliveryChallanItem
                        {
                            Id = Guid.NewGuid(),
                            ProductId = item.ProductId,
                            ProductName = product?.Name ?? "Unknown Product",
                            Qty = item.Quantity,
                            Unit = product?.Unit ?? "PCS",
                            Rate = rate,
                            MRP = product?.MRP ?? 0M,
                            DiscountPercent = 0M,
                            DiscountAmount = 0M,
                            GSTPercent = gstPercent,
                            TaxAmount = taxAmount,
                            Total = itemTotal,
                            WarehouseId = header.FromWarehouseId,
                            BatchNumber = item.BatchNumber,
                            CompanyId = companyId,
                            BranchId = header.FromBranchId
                        });
                    }

                    // D. Generate Challan Number
                    var lastChallan = await _context.DeliveryChallans
                        .IgnoreQueryFilters()
                        .Where(x => x.CompanyId == companyId)
                        .OrderByDescending(x => x.CreatedOn)
                        .FirstOrDefaultAsync();

                    int nextId = 1;
                    if (lastChallan != null && !string.IsNullOrEmpty(lastChallan.ChallanNo))
                    {
                        var parts = lastChallan.ChallanNo.Split('/');
                        if (parts.Length > 0 && int.TryParse(parts.Last(), out int parsedId))
                        {
                            nextId = parsedId + 1;
                        }
                    }
                    string fyString = $"{DateTime.Now.Year}-{(DateTime.Now.Year + 1).ToString().Substring(2)}";
                    string challanNo = $"DC/{fyString}/{nextId:D4}";

                    // E. Create Delivery Challan Header
                    var challan = new DeliveryChallan
                    {
                        Id = Guid.NewGuid(),
                        ChallanNo = challanNo,
                        ChallanDate = header.TransferDate,
                        StockTransferHeaderId = header.Id,
                        CustomerId = null,
                        CustomerName = $"Stock Transfer: {toWarehouse?.Name ?? header.ToBranchId}",
                        SubTotal = subTotal,
                        TotalTax = totalTax,
                        GrandTotal = subTotal + totalTax,
                        Remarks = $"Internal Stock Transfer from {fromWarehouse?.Name ?? "Source"} to {toWarehouse?.Name ?? "Destination"}. Ref: {header.TransferNumber}",
                        Status = "Pending",
                        VehicleRegNo = header.VehicleRegNo,
                        Origin = fromWarehouse?.Name ?? header.FromBranchId,
                        Destination = toWarehouse?.Name ?? header.ToBranchId,
                        CompanyId = companyId,
                        BranchId = header.FromBranchId,
                        Items = challanItems
                    };

                    await _context.DeliveryChallans.AddAsync(challan);
                    await _context.SaveChangesAsync();

                    // F. Add native In-App Notification for Destination Branch
                    await _notificationRepository.AddNotificationAsync(
                        "Incoming Stock Transfer",
                        $"New stock transfer {header.TransferNumber} dispatched to your branch from source branch.",
                        "Inventory",
                        "/app/inventory/item-transfer",
                        header.ToBranchId,
                        companyId
                    );

                     await transaction.CommitAsync();

                    // G. Trigger Background Email Dispatch
                    try
                    {
                        var company = await _companyClient.GetCompanyProfileAsync();
                        if (company != null)
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    using var scope = _scopeFactory.CreateScope();
                                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                                    var pdfService = scope.ServiceProvider.GetRequiredService<IPdfService>();
                                    var scopedContext = scope.ServiceProvider.GetRequiredService<IInventoryDbContext>();

                                    // Find target and source branch names and emails
                                    var targetBranch = company.Addresses.FirstOrDefault(a => a.Id.ToString() == header.ToBranchId);
                                    var sourceBranch = company.Addresses.FirstOrDefault(a => a.Id.ToString() == header.FromBranchId);

                                    string targetEmail = targetBranch?.Email ?? "";
                                    string fromBranchName = sourceBranch?.BranchName ?? "Khanpur Branch";
                                    string toBranchName = targetBranch?.BranchName ?? "Matihaan Branch";

                                    if (!string.IsNullOrEmpty(targetEmail))
                                    {
                                        // Fetch the newly created delivery challan with items inside scope
                                        var dbChallan = await scopedContext.DeliveryChallans
                                            .IgnoreQueryFilters()
                                            .Include(dc => dc.Items)
                                            .FirstOrDefaultAsync(dc => dc.StockTransferHeaderId == header.Id);

                                        if (dbChallan != null)
                                        {
                                            byte[] pdfBytes = null;
                                            try
                                            {
                                                string challanHtml = GenerateChallanHtml(company, fromBranchName, toBranchName, dbChallan);
                                                pdfBytes = pdfService.Convert(challanHtml);
                                            }
                                            catch (Exception pdfEx)
                                            {
                                                Console.WriteLine($"[StockTransferRepository] PDF generation failed: {pdfEx.Message}");
                                            }

                                            await emailService.SendStockTransferEmailAsync(
                                                company,
                                                targetEmail,
                                                header.TransferNumber,
                                                fromBranchName,
                                                toBranchName,
                                                dbChallan.ChallanNo!,
                                                pdfBytes
                                            );
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[StockTransferRepository] Background email dispatch failed: {ex.Message}");
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[StockTransferRepository] Failed to kick off background task: {ex.Message}");
                    }

                    return header.TransferNumber;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        private static string GenerateChallanHtml(CompanyProfileDto company, string fromBranchName, string toBranchName, DeliveryChallan challan)
        {
            var sb = new System.Text.StringBuilder();
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
                            <strong style='color: #2563eb; font-size: 14px;'>Dispatched From (Source):</strong><br/>
                            <strong>{fromBranchName}</strong><br/>
                            {company.Name}<br/>
                            GSTIN: {company.Gstin}<br/>
                            Phone: {company.PrimaryPhone}<br/>
                            Email: {company.PrimaryEmail}
                        </td>
                        <td style='text-align: right;'>
                            <strong style='color: #2563eb; font-size: 14px;'>Dispatched To (Destination):</strong><br/>
                            <strong>{toBranchName}</strong><br/>
                            {company.Name}<br/>
                            GSTIN: {company.Gstin}
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
                    Internal stock transfer between branches. No sale is involved.<br/>
                    <em>Regd. office: {address}</em>
                </div>
            </div>
        </body>
        </html>");

            return sb.ToString();
        }

        public async Task<bool> ReceiveTransferAsync(Guid id, string? remarks)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var companyId = _currentUserService.CompanyId ?? Guid.Empty;

                    // 1. Fetch the transfer header with details
                    var transfer = await _context.StockTransferHeaders
                        .IgnoreQueryFilters()
                        .Include(h => h.Items)
                        .FirstOrDefaultAsync(h => h.Id == id && h.CompanyId == companyId);

                    if (transfer == null)
                    {
                        throw new Exception("Stock transfer record not found.");
                    }

                    // 2. Transition state and record remarks via clean domain method
                    transfer.ReceiveTransfer(remarks);

                    // 3. For each item, add to destination warehouse and record Transfer-In (Step 2: Add to Destination Warehouse)
                    foreach (var item in transfer.Items)
                    {
                        var destStock = await _context.WarehouseStocks
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == transfer.ToWarehouseId && ws.CompanyId == companyId);

                        if (destStock != null)
                        {
                            destStock.Quantity += item.Quantity;
                        }
                        else
                        {
                            await _context.WarehouseStocks.AddAsync(new WarehouseStock
                            {
                                ProductId = item.ProductId,
                                WarehouseId = transfer.ToWarehouseId,
                                Quantity = item.Quantity,
                                CompanyId = companyId,
                                BranchId = transfer.ToBranchId
                            });
                        }

                        // Record IN to Destination
                        await _context.InventoryTransactions.AddAsync(new InventoryTransaction(
                            item.ProductId,
                            item.Quantity,
                            "Transfer-In",
                            transfer.TransferNumber,
                            transfer.ToWarehouseId,
                            null,
                            null, null,
                            companyId,
                            transfer.ToBranchId,
                            null,
                            item.BatchNumber
                        ));
                    }

                    await _context.SaveChangesAsync();

                    // 4. Create AppNotification for the SOURCE branch to let them know it has been received
                    await _notificationRepository.AddNotificationAsync(
                        "Stock Transfer Received",
                        $"Stock transfer {transfer.TransferNumber} has been received successfully by the destination branch.",
                        "Inventory",
                        "/app/inventory/item-transfer",
                        transfer.FromBranchId,
                        companyId
                    );

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<IEnumerable<StockTransferHeader>> GetTransferListAsync()
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            return await _context.StockTransferHeaders
                .IgnoreQueryFilters()
                .Include(h => h.FromWarehouse)
                .Include(h => h.ToWarehouse)
                .Where(h => h.CompanyId == companyId)
                .OrderByDescending(h => h.CreatedOn)
                .ToListAsync();
        }

        public async Task<StockTransferHeader?> GetTransferByIdAsync(Guid id)
        {
            return await _context.StockTransferHeaders
                .IgnoreQueryFilters()
                .Include(h => h.FromWarehouse)
                .Include(h => h.ToWarehouse)
                .Include(h => h.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(h => h.Id == id);
        }
    }
}
