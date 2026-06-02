using ClosedXML.Excel;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.GRN.DTOs.Stock;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Repositories
{
    public class StockRepository : IStockRepository
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly InventoryDbContext _context;

        public StockRepository(InventoryDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public Task<StockRefillDetailsDto> GetRefillDetailsAsync(Guid productId)
        {
            throw new NotImplementedException();
        }

        public async Task<StockPagedResponseDto> GetCurrentStockAsync(
            string? search,
            string? sortField,
            string? sortOrder,
            int pageIndex,
            int pageSize,
            DateTime? startDate = null,
            DateTime? endDate = null,
            Guid? warehouseId = null,
            Guid? rackId = null,
            bool showPurged = false,
            string? branchId = null)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var finalBranchId = !string.IsNullOrEmpty(branchId) ? branchId : _currentUserService.BranchId;

            // STEP 1: Base Query - Start from Products to ensure all items are included
            var baseQuery = _context.Products.IgnoreQueryFilters().Include(p => p.Category).AsNoTracking().AsQueryable();

            if (!_currentUserService.IsPlatformAdmin)
            {
                baseQuery = baseQuery.Where(p => p.CompanyId == companyId);
            }

            var rawGrns = _context.GRNDetails.IgnoreQueryFilters().AsNoTracking()
                .Where(g => g.GRNHeader.Status != "Cancelled")
                .Select(g => new
                {
                    ProductId = g.ProductId,
                    WarehouseId = (Guid?)g.WarehouseId,
                    RackId = (Guid?)g.RackId,
                    BranchId = g.BranchId,
                    ReceivedQty = g.ReceivedQty,
                    RejectedQty = g.RejectedQty,
                    ModifiedOn = g.ModifiedOn,
                    GRNId = (Guid?)g.Id,
                    IsTransfer = false,
                    GrnRackName = g.Rack != null ? g.Rack.Name : null,
                    GrnRackDescription = g.Rack != null ? g.Rack.Description : null,
                    GrnUnitRate = g.UnitRate,
                    GrnPurchaseOrderId = g.GRNHeader != null ? (Guid?)g.GRNHeader.PurchaseOrderId : null,
                    GrnBatchNumber = g.BatchNumber
                });

            var rawTransfers = _context.StockTransferDetails.IgnoreQueryFilters().AsNoTracking()
                .Where(td => td.StockTransferHeader.Status == "Completed")
                .Select(td => new
                {
                    ProductId = td.ProductId,
                    WarehouseId = (Guid?)td.StockTransferHeader.ToWarehouseId,
                    RackId = (Guid?)null,
                    BranchId = td.StockTransferHeader.ToBranchId,
                    ReceivedQty = 0M,
                    RejectedQty = 0M,
                    ModifiedOn = td.StockTransferHeader.ModifiedOn ?? td.StockTransferHeader.CreatedOn,
                    GRNId = (Guid?)null,
                    IsTransfer = true,
                    GrnRackName = (string?)null,
                    GrnRackDescription = (string?)null,
                    GrnUnitRate = 0M,
                    GrnPurchaseOrderId = (Guid?)null,
                    GrnBatchNumber = (string?)null
                });

            var rawInputs = rawGrns.Concat(rawTransfers);

            var finalQuery = rawInputs
                .Join(_context.Products.IgnoreQueryFilters().Include(p => p.Category).AsNoTracking(), 
                    ri => ri.ProductId, p => p.Id, (ri, p) => new { ri, p })
                .Select(x => new
                {
                    ProductId = x.p.Id,
                    ProductName = x.p.Name,
                    CategoryName = x.p.Category != null ? x.p.Category.CategoryName : "N/A",
                    UnitName = x.p.Unit,
                    MinStock = x.p.MinStock,
                    WarehouseId = x.ri.WarehouseId,
                    WarehouseName = _context.Warehouses.IgnoreQueryFilters().Where(w => w.Id == x.ri.WarehouseId).Select(w => w.Name).FirstOrDefault() ?? "N/A",
                    RackId = x.ri.RackId,
                    RackName = _context.Racks.IgnoreQueryFilters().Where(r => r.Id == x.ri.RackId).Select(r => r.Name).FirstOrDefault() ?? "N/A",
                    Sku = x.p.Sku,
                    GstPercent = x.p.DefaultGst ?? 0M,
                    HSNCode = x.p.HSNCode,
                    IsExpiryRequired = x.p.IsExpiryRequired,
                    MRP = x.p.MRP,
                    Discount = x.p.Discount,
                    SaleRate = x.p.SaleRate ?? 0M,
                    BasePurchasePrice = x.p.BasePurchasePrice,
                    BranchId = x.ri.BranchId,
                    ReceivedQty = x.ri.ReceivedQty,
                    RejectedQty = x.ri.RejectedQty,
                    ModifiedOn = (DateTime?)x.ri.ModifiedOn,
                    IsTransfer = x.ri.IsTransfer,
                    GRNId = x.ri.GRNId,
                    GrnRackName = x.ri.GrnRackName,
                    GrnRackDescription = x.ri.GrnRackDescription,
                    GrnUnitRate = x.ri.GrnUnitRate,
                    GrnPurchaseOrderId = x.ri.GrnPurchaseOrderId,
                    GrnBatchNumber = x.ri.GrnBatchNumber
                });

            if (!_currentUserService.IsPlatformAdmin && !string.IsNullOrEmpty(finalBranchId))
            {
                finalQuery = finalQuery.Where(x => x.BranchId == finalBranchId || x.BranchId == null);
            }

            if (startDate.HasValue)
                finalQuery = finalQuery.Where(x => x.ModifiedOn != null && x.ModifiedOn.Value.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                finalQuery = finalQuery.Where(x => x.ModifiedOn != null && x.ModifiedOn.Value.Date <= endDate.Value.Date);

            if (warehouseId.HasValue && warehouseId.Value != Guid.Empty)
                finalQuery = finalQuery.Where(x => x.WarehouseId == warehouseId.Value);
            if (rackId.HasValue && rackId.Value != Guid.Empty)
                finalQuery = finalQuery.Where(x => x.RackId == rackId.Value);

            // STEP 2: Grouping Logic
            var groupedQuery = finalQuery
                .GroupBy(g => new
                {
                    g.ProductId,
                    ProductName = g.ProductName,
                    CategoryName = g.CategoryName,
                    UnitName = g.UnitName,
                    MinStock = g.MinStock,
                    g.WarehouseId,
                    WarehouseName = g.WarehouseName,
                    g.RackId,
                    RackName = g.RackName,
                    Sku = g.Sku,
                    GstPercent = g.GstPercent,
                    g.IsExpiryRequired,
                    g.MRP,
                    g.Discount,
                    g.SaleRate,
                    g.BasePurchasePrice,
                    BranchId = g.BranchId,
                    HsnCode = g.HSNCode
                })
                .Select(group => new StockSummaryDto
                {
                    ProductId = group.Key.ProductId,
                    ProductName = group.Key.ProductName,
                    CategoryName = group.Key.CategoryName,
                    Unit = group.Key.UnitName,
                    MinStockLevel = group.Key.MinStock,
                    WarehouseId = group.Key.WarehouseId,
                    WarehouseName = group.Key.WarehouseName,
                    RackId = group.Key.RackId,
                    RackName = group.Key.RackName,
                    Sku = group.Key.Sku,
                    GstPercent = group.Key.GstPercent,
                    IsExpiryRequired = group.Key.IsExpiryRequired,
                    MRP = group.Key.MRP,
                    Discount = group.Key.Discount,
                    SaleRate = group.Key.SaleRate,
                    BasePurchasePrice = group.Key.BasePurchasePrice,
                    BranchId = group.Key.BranchId,
                    HsnCode = group.Key.HsnCode,
                    TotalReceived = group.Sum(x => x.ReceivedQty),
                    TotalRejected = group.Sum(x => (x.GrnRackName != null && (
                        x.GrnRackName.ToLower().Contains("e1") || 
                        x.GrnRackName.ToLower().StartsWith("e -") || 
                        x.GrnRackName.ToLower().StartsWith("e-") ||
                        (x.GrnRackDescription != null && (
                            x.GrnRackDescription.ToLower().Contains("expired") || 
                            x.GrnRackDescription.ToLower().Contains("damaged") || 
                            x.GrnRackDescription.ToLower().Contains("rejected") ||
                            x.GrnRackDescription.ToLower().Contains("purged")
                        ))
                    )) ? 0 : x.RejectedQty),
                    TotalExpired = group.Sum(x => (x.GrnRackName != null && (
                        x.GrnRackName.ToLower().Contains("e1") || 
                        x.GrnRackName.ToLower().StartsWith("e -") || 
                        x.GrnRackName.ToLower().StartsWith("e-") ||
                        (x.GrnRackDescription != null && (
                            x.GrnRackDescription.ToLower().Contains("expired") || 
                            x.GrnRackDescription.ToLower().Contains("damaged") || 
                            x.GrnRackDescription.ToLower().Contains("rejected") ||
                            x.GrnRackDescription.ToLower().Contains("purged")
                        ))
                    )) ? x.RejectedQty : 0),
                    AvailableStock = group.Sum(x => x.ReceivedQty - x.RejectedQty),
                    IsAlreadyPurged = !group.Any(x => x.IsTransfer) && group.Sum(x => x.ReceivedQty) == 0 && group.Sum(x => x.RejectedQty) == 0,
                    IsTransferInput = group.Any(x => x.IsTransfer),
                    PurgedDate = (!group.Any(x => x.IsTransfer) && group.Sum(x => x.ReceivedQty) == 0 && group.Sum(x => x.RejectedQty) == 0) ? group.Max(x => x.ModifiedOn) : null,
                    LastRate = group.OrderByDescending(x => x.GRNId).Select(x => x.GrnUnitRate).FirstOrDefault(),
                    LastPurchaseOrderId = group.OrderByDescending(x => x.GRNId).Select(x => x.GrnPurchaseOrderId).FirstOrDefault(),
                    BatchNumber = group.OrderByDescending(x => x.GRNId).Select(x => x.GrnBatchNumber).FirstOrDefault()
                });

            if (!showPurged)
            {
                groupedQuery = groupedQuery.Where(x => x.TotalReceived > 0 || x.TotalRejected > 0 || x.IsTransferInput);
            }
            else
            {
                groupedQuery = groupedQuery.Where(x => 
                    x.TotalReceived > 0 || 
                    x.TotalRejected > 0 || 
                    x.IsTransferInput ||
                    (x.RackName.ToLower().Contains("e1") || x.RackName.ToLower().StartsWith("e -") || x.RackName.ToLower().StartsWith("e-") || (x.RackName != null && x.RackName.ToLower().Contains("expired")))
                );
            }

            if (!string.IsNullOrEmpty(search))
            {
                if (Guid.TryParse(search, out Guid searchGuid))
                {
                    groupedQuery = groupedQuery.Where(x => x.ProductId == searchGuid);
                }
                else
                {
                    groupedQuery = groupedQuery.Where(x => x.ProductName.Contains(search) || (x.Sku != null && x.Sku.Contains(search)));
                }
            }

            bool isDesc = sortOrder?.ToLower() == "desc";
            groupedQuery = sortField?.ToLower() switch
            {
                "productname" => isDesc ? groupedQuery.OrderByDescending(x => x.ProductName) : groupedQuery.OrderBy(x => x.ProductName),
                "totalreceived" => isDesc ? groupedQuery.OrderByDescending(x => x.TotalReceived) : groupedQuery.OrderBy(x => x.TotalReceived),
                "availablestock" => isDesc ? groupedQuery.OrderByDescending(x => x.AvailableStock) : groupedQuery.OrderBy(x => x.AvailableStock),
                _ => groupedQuery.OrderBy(x => x.ProductName)
            };

            var totalCount = await groupedQuery.CountAsync();
            var items = await groupedQuery.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();

            // STEP 4: Real-Time Stats (Net Sale Calculation)
            foreach (var item in items)
            {
                var salesQuery = _context.SaleOrderItems.IgnoreQueryFilters().AsQueryable();
                var invoiceQuery = _context.SalesInvoiceItems.IgnoreQueryFilters().AsQueryable();
                var returnsQuery = _context.SaleReturnItems.IgnoreQueryFilters().AsQueryable();
                var grnQuery = _context.GRNDetails.IgnoreQueryFilters()
                    .Where(g => g.GRNHeader.Status != "Cancelled")
                    .AsQueryable();

                if (!_currentUserService.IsPlatformAdmin)
                {
                    salesQuery = salesQuery.Where(si => si.CompanyId == companyId);
                    invoiceQuery = invoiceQuery.Where(ii => ii.CompanyId == companyId);
                    returnsQuery = returnsQuery.Where(sri => sri.CompanyId == companyId);
                    grnQuery = grnQuery.Where(g => g.CompanyId == companyId);
                }

                salesQuery = salesQuery.Where(si => si.ProductId == item.ProductId);
                invoiceQuery = invoiceQuery.Where(ii => ii.ProductId == item.ProductId);
                returnsQuery = returnsQuery.Where(sri => sri.ProductId == item.ProductId);
                grnQuery = grnQuery.Where(g => g.ProductId == item.ProductId);

                if (!_currentUserService.IsPlatformAdmin && !string.IsNullOrEmpty(finalBranchId))
                {
                    salesQuery = salesQuery.Where(si => si.BranchId == finalBranchId);
                    invoiceQuery = invoiceQuery.Where(ii => ii.BranchId == finalBranchId);
                    returnsQuery = returnsQuery.Where(sri => sri.BranchId == finalBranchId);
                    grnQuery = grnQuery.Where(g => g.BranchId == finalBranchId);
                }

                var grossSold = await salesQuery
                    .Where(si => si.WarehouseId == item.WarehouseId && si.RackId == item.RackId 
                        && si.SaleOrder.Status != "Draft" && si.SaleOrder.Status != "Cancelled" && si.SaleOrder.Status != "Canceled")
                    .SumAsync(si => (decimal?)si.Qty) ?? 0;

                var quickSold = await invoiceQuery
                    .Where(ii => ii.WarehouseId == item.WarehouseId && ii.RackId == item.RackId 
                        && ii.SalesInvoice.Status != "Draft" && ii.SalesInvoice.Status != "Cancelled" && ii.SalesInvoice.Status != "Canceled")
                    .SumAsync(ii => (decimal?)ii.Qty) ?? 0;

                grossSold += quickSold;

                var totalSaleReturn = await returnsQuery
                    .Where(sri => sri.WarehouseId == item.WarehouseId && sri.RackId == item.RackId && (sri.SaleReturnHeader.Status == "Confirmed" || sri.SaleReturnHeader.Status == "INWARDED"))
                    .SumAsync(sri => (decimal?)sri.ReturnQty) ?? 0;

                var prItemsQuery = _context.PurchaseReturnItems.IgnoreQueryFilters().AsQueryable();
                if (!_currentUserService.IsPlatformAdmin)
                {
                    prItemsQuery = prItemsQuery.Where(pri => pri.CompanyId == companyId);
                }

                var purchaseReturns = await prItemsQuery
                    .Where(pri => pri.ProductId == item.ProductId && pri.WarehouseId == item.WarehouseId && pri.RackId == item.RackId)
                    .Select(pri => new { pri.ReturnQty, pri.GrnRef })
                    .ToListAsync();

                decimal totalPurchaseReturn = 0;
                decimal totalDeductibleReturn = 0;

                foreach(var pr in purchaseReturns)
                {
                    totalPurchaseReturn += (decimal)pr.ReturnQty;
                    
                    // Find the original GRN to check if this was a rejected item
                    var gd = await grnQuery.FirstOrDefaultAsync(g => g.GRNHeader.GRNNumber == pr.GrnRef);
                    decimal rejected = gd?.RejectedQty ?? 0;
                    
                    // Only deduct from stock if the returned quantity exceeds the rejected quantity
                    totalDeductibleReturn += Math.Max(0, (decimal)pr.ReturnQty - rejected);
                }

                var transactionsQuery = _context.InventoryTransactions.IgnoreQueryFilters().AsQueryable();
                if (!_currentUserService.IsPlatformAdmin)
                {
                    transactionsQuery = transactionsQuery.Where(tx => tx.CompanyId == companyId);
                }

                var itemTransactions = await transactionsQuery
                    .Where(tx => tx.ProductId == item.ProductId && tx.WarehouseId == item.WarehouseId && tx.RackId == item.RackId)
                    .ToListAsync();

                var totalPurged = Math.Abs(itemTransactions
                    .Where(tx => tx.TransactionType == "StockPurge-OUT")
                    .Sum(tx => tx.Quantity));

                if (totalPurged > 0)
                {
                    if (item.IsAlreadyPurged)
                    {
                        item.TotalExpired = totalPurged;
                        item.TotalReceived = totalPurged;
                    }
                    else
                    {
                        item.TotalExpired += totalPurged;
                    }
                }

                // 🚀 TRANSFER CALCULATION
                var transfersOutQuery = _context.StockTransferDetails.IgnoreQueryFilters().AsQueryable();
                if (!_currentUserService.IsPlatformAdmin)
                {
                    transfersOutQuery = transfersOutQuery.Where(td => td.CompanyId == companyId);
                }

                var transferredOut = await transfersOutQuery
                    .Where(td => td.ProductId == item.ProductId && td.StockTransferHeader.FromWarehouseId == item.WarehouseId)
                    .Where(td => 
                        _context.GRNDetails.IgnoreQueryFilters().Any(g => 
                            g.ProductId == td.ProductId && 
                            g.WarehouseId == item.WarehouseId && 
                            g.RackId == item.RackId && 
                            (g.GRNHeader.GRNNumber == td.BatchNumber || g.BatchNumber == td.BatchNumber)
                        ) ||
                        (item.RackId == null && 
                         !_context.GRNDetails.IgnoreQueryFilters().Any(g => 
                             g.ProductId == td.ProductId && 
                             g.WarehouseId == item.WarehouseId && 
                             (g.GRNHeader.GRNNumber == td.BatchNumber || g.BatchNumber == td.BatchNumber)
                         ) &&
                         _context.StockTransferDetails.IgnoreQueryFilters().Any(tin =>
                             tin.ProductId == td.ProductId &&
                             tin.StockTransferHeader.ToWarehouseId == item.WarehouseId &&
                             tin.StockTransferHeader.Status == "Completed" &&
                             tin.BatchNumber == td.BatchNumber
                         ))
                    )
                    .SumAsync(td => (decimal?)td.Quantity) ?? 0;

                var transfersInQuery = _context.StockTransferDetails.IgnoreQueryFilters().AsQueryable();
                if (!_currentUserService.IsPlatformAdmin)
                {
                    transfersInQuery = transfersInQuery.Where(td => td.CompanyId == companyId);
                }

                var transferredIn = (item.RackId == null) ? (await transfersInQuery
                    .Where(td => td.ProductId == item.ProductId && td.StockTransferHeader.ToWarehouseId == item.WarehouseId)
                    .SumAsync(td => (decimal?)td.Quantity) ?? 0) : 0;

                var unlinkedSales = await salesQuery
                    .Where(si => (si.WarehouseId == null || si.RackId == null) 
                        && si.SaleOrder.Status != "Draft" && si.SaleOrder.Status != "Cancelled" && si.SaleOrder.Status != "Canceled")
                    .SumAsync(si => (decimal?)si.Qty) ?? 0;

                var unlinkedQuickSold = await invoiceQuery
                    .Where(ii => (ii.WarehouseId == null || ii.RackId == null) 
                        && ii.SalesInvoice.Status != "Draft" && ii.SalesInvoice.Status != "Cancelled" && ii.SalesInvoice.Status != "Canceled")
                    .SumAsync(ii => (decimal?)ii.Qty) ?? 0;

                unlinkedSales += unlinkedQuickSold;

                var isOldest = await grnQuery
                    .OrderBy(g => g.GRNHeader.ReceivedDate)
                    .Select(g => new { g.WarehouseId, g.RackId })
                    .FirstOrDefaultAsync();

                var adjustment = (isOldest != null && isOldest.WarehouseId == item.WarehouseId && isOldest.RackId == item.RackId) ? unlinkedSales : 0;

                // Deduct pending/draft Delivery Challans that have not yet been invoiced (to prevent double deduction and show instant stock drop)
                var challanQuery = _context.DeliveryChallanItems.IgnoreQueryFilters().AsQueryable();
                if (!_currentUserService.IsPlatformAdmin)
                {
                    challanQuery = challanQuery.Where(dci => dci.CompanyId == companyId);
                }
                challanQuery = challanQuery.Where(dci => dci.ProductId == item.ProductId);
                if (!_currentUserService.IsPlatformAdmin && !string.IsNullOrEmpty(finalBranchId))
                {
                    challanQuery = challanQuery.Where(dci => dci.BranchId == finalBranchId);
                }

                var pendingChallanQty = await challanQuery
                    .Where(dci => dci.WarehouseId == item.WarehouseId && dci.RackId == item.RackId
                        && (dci.DeliveryChallan.Status == "Pending" || dci.DeliveryChallan.Status == "Draft"))
                    .SumAsync(dci => (decimal?)dci.Qty) ?? 0;

                item.TotalSold = grossSold + adjustment - totalSaleReturn + pendingChallanQty;
                item.TotalReturned = totalPurchaseReturn;
                item.TotalTransferredOut = transferredOut;
                item.TotalTransferredIn = transferredIn;
                if (transferredOut > 0)
                {
                    item.TransferredBranchId = await transfersOutQuery
                        .Where(td => td.ProductId == item.ProductId && td.StockTransferHeader.FromWarehouseId == item.WarehouseId)
                        .Select(td => td.StockTransferHeader.ToBranchId)
                        .FirstOrDefaultAsync();
                }
                item.AvailableStock = item.TotalReceived - item.TotalRejected - item.TotalSold - totalDeductibleReturn - transferredOut + transferredIn - totalPurged;
                if (item.AvailableStock < 0)
                {
                    item.AvailableStock = 0;
                }

                if (item.TotalReceived == 0 && transferredIn > 0)
                {
                    item.TotalReceived = transferredIn;
                }

                if ((string.IsNullOrEmpty(item.RackName) || item.RackName == "N/A") && transferredIn > 0)
                {
                    item.RackName = "Transferred In";
                }

                var earliestBatch = await grnQuery
                    .Where(g => g.WarehouseId == item.WarehouseId && g.RackId == item.RackId)
                    .OrderBy(g => g.ExpDate ?? DateTime.MaxValue)
                    .ThenBy(g => g.GRNHeader.ReceivedDate)
                    .Select(g => new { g.MfgDate, g.ExpDate, ReceivedDate = g.GRNHeader.ReceivedDate, BatchNumber = g.GRNHeader.GRNNumber })
                    .FirstOrDefaultAsync();

                if (earliestBatch != null)
                {
                    item.ManufacturingDate = earliestBatch.MfgDate;
                    item.ExpiryDate = earliestBatch.ExpDate;
                    item.ReceivedDate = earliestBatch.ReceivedDate;
                    item.BatchNumber = earliestBatch.BatchNumber; // Use GRN/Batch number
                }
                else if (transferredIn > 0)
                {
                    // Find the earliest active transfer-in detail to populate dates
                    var earliestTransfer = await transfersInQuery
                        .Where(td => td.ProductId == item.ProductId && td.StockTransferHeader.ToWarehouseId == item.WarehouseId)
                        .OrderBy(td => td.StockTransferHeader.TransferDate)
                        .Select(td => new { td.BatchNumber, td.StockTransferHeader.TransferDate })
                        .FirstOrDefaultAsync();

                    if (earliestTransfer != null)
                    {
                        var originalBatch = await _context.GRNDetails.IgnoreQueryFilters()
                            .Where(g => g.ProductId == item.ProductId && (g.GRNHeader.GRNNumber == earliestTransfer.BatchNumber || g.BatchNumber == earliestTransfer.BatchNumber))
                            .Select(g => new { g.MfgDate, g.ExpDate })
                            .FirstOrDefaultAsync();

                        if (originalBatch != null)
                        {
                            item.ManufacturingDate = originalBatch.MfgDate;
                            item.ExpiryDate = originalBatch.ExpDate;
                        }
                        item.ReceivedDate = earliestTransfer.TransferDate;
                        item.BatchNumber = earliestTransfer.BatchNumber;
                    }
                }

                var grnHistory = await grnQuery
                    .Where(g => g.ProductId == item.ProductId && g.WarehouseId == item.WarehouseId && g.RackId == item.RackId)
                    .Select(allG => new StockHistoryDto
                    {
                        ProductId = allG.ProductId,
                        WarehouseId = allG.WarehouseId,
                        RackId = allG.RackId,
                        ReceivedDate = allG.GRNHeader.ReceivedDate,
                        PONumber = allG.GRNHeader.PurchaseOrder.PoNumber,
                        GRNNumber = allG.GRNHeader.GRNNumber,
                        SupplierName = allG.GRNHeader.PurchaseOrder.SupplierName ?? "N/A",
                        TransactionType = allG.GRNHeader.IsQuick ? "QuickGRN" : "GRN",
                        ProductName = allG.Product.Name,
                        ReceivedQty = allG.ReceivedQty,
                        ExpiredQty = (allG.Rack != null && (
                            allG.Rack.Name.ToLower().Contains("e1") || 
                            allG.Rack.Name.ToLower().StartsWith("e -") || 
                            allG.Rack.Name.ToLower().StartsWith("e-") ||
                            (allG.Rack.Description != null && (
                                allG.Rack.Description.ToLower().Contains("expired") || 
                                allG.Rack.Description.ToLower().Contains("damaged") || 
                                allG.Rack.Description.ToLower().Contains("rejected") ||
                                allG.Rack.Description.ToLower().Contains("purged")
                            ))
                        )) ? allG.RejectedQty : 0,
                        RejectedQty = (allG.Rack != null && (
                            allG.Rack.Name.ToLower().Contains("e1") || 
                            allG.Rack.Name.ToLower().StartsWith("e -") || 
                            allG.Rack.Name.ToLower().StartsWith("e-") ||
                            (allG.Rack.Description != null && (
                                allG.Rack.Description.ToLower().Contains("expired") || 
                                allG.Rack.Description.ToLower().Contains("damaged") || 
                                allG.Rack.Description.ToLower().Contains("rejected") ||
                                allG.Rack.Description.ToLower().Contains("purged")
                            ))
                        )) ? 0 : allG.RejectedQty,
                        ManufacturingDate = allG.MfgDate,
                        ExpiryDate = allG.ExpDate,
                        IsExpiryRequired = allG.Product.IsExpiryRequired,
                        WarehouseName = allG.Warehouse != null ? allG.Warehouse.Name : "N/A",
                        RackName = allG.Rack != null ? allG.Rack.Name : "N/A",
                        AvailableQty = allG.ReceivedQty - allG.RejectedQty, 
                        CurrentStock = item.AvailableStock, 
                        TotalReturned = totalPurchaseReturn,
                        BranchId = allG.BranchId,
                        BranchName = allG.BranchId,
                        BatchNumber = allG.BatchNumber,
                        ReferenceNumber = allG.ReferenceNumber ?? allG.GRNHeader.PurchaseOrder.PoNumber,
                        IsAlreadyPurged = allG.ReceivedQty == 0 && allG.RejectedQty == 0
                    })
                    .ToListAsync();

                // 🚀 ADD TRANSFERS TO HISTORY
                var transferInHistory = item.RackId == null ? await _context.StockTransferDetails.IgnoreQueryFilters()
                    .Where(td => td.CompanyId == companyId && td.ProductId == item.ProductId && td.StockTransferHeader.ToWarehouseId == item.WarehouseId)
                    .Select(td => new StockHistoryDto
                    {
                        ProductId = td.ProductId,
                        WarehouseId = item.WarehouseId,
                        ReceivedDate = td.StockTransferHeader.TransferDate,
                        PONumber = td.StockTransferHeader.TransferNumber,
                        GRNNumber = "TRANSFER-IN",
                        SupplierName = "From: " + td.StockTransferHeader.FromWarehouse.Name,
                        TransactionType = "Transfer",
                        ProductName = item.ProductName,
                        ReceivedQty = td.Quantity,
                        TransferredQty = td.Quantity,
                        AvailableQty = td.Quantity,
                        WarehouseName = td.StockTransferHeader.FromWarehouse.Name, // 🎯 Source Warehouse
                        RackName = "Transferred In",
                        BranchId = td.StockTransferHeader.FromWarehouse.BranchId, // 🎯 Source Branch ID
                        BranchName = td.StockTransferHeader.FromWarehouse.BranchId, // 🎯 Source Branch Name
                        IsExpiryRequired = _context.Products.Where(p => p.Id == td.ProductId).Select(p => p.IsExpiryRequired).FirstOrDefault(),
                        ExpiryDate = _context.GRNDetails.Where(g => g.ProductId == td.ProductId && g.GRNHeader.GRNNumber == td.BatchNumber).Select(g => g.ExpDate).FirstOrDefault(),
                        ManufacturingDate = _context.GRNDetails.Where(g => g.ProductId == td.ProductId && g.GRNHeader.GRNNumber == td.BatchNumber).Select(g => g.MfgDate).FirstOrDefault(),
                        BatchNumber = td.BatchNumber,
                        ReferenceNumber = td.StockTransferHeader.TransferNumber,
                        CurrentStock = item.AvailableStock
                    })
                    .ToListAsync() : new List<StockHistoryDto>();

                var transferOutHistory = await _context.StockTransferDetails.IgnoreQueryFilters()
                    .Where(td => td.CompanyId == companyId && td.ProductId == item.ProductId && td.StockTransferHeader.FromWarehouseId == item.WarehouseId)
                    .Where(td => _context.GRNDetails.IgnoreQueryFilters().Any(g => 
                        g.ProductId == td.ProductId && 
                        g.WarehouseId == item.WarehouseId && 
                        g.RackId == item.RackId && 
                        (g.GRNHeader.GRNNumber == td.BatchNumber || g.BatchNumber == td.BatchNumber)
                    ))
                    .Select(td => new StockHistoryDto
                    {
                        ProductId = td.ProductId,
                        WarehouseId = item.WarehouseId,
                        ReceivedDate = td.StockTransferHeader.TransferDate,
                        PONumber = td.StockTransferHeader.TransferNumber,
                        GRNNumber = "TRANSFER-OUT",
                        SupplierName = "To: " + td.StockTransferHeader.ToWarehouse.Name,
                        TransactionType = "Transfer",
                        ProductName = item.ProductName,
                        ReceivedQty = 0,
                        SoldQty = 0, 
                        TransferredQty = td.Quantity,
                        AvailableQty = -td.Quantity,
                        WarehouseName = item.WarehouseName,
                        RackName = item.RackName,
                        BranchId = td.BranchId ?? td.StockTransferHeader.FromWarehouse.BranchId, // Fallback to warehouse branch
                        ExpiryDate = _context.GRNDetails.Where(g => g.ProductId == td.ProductId && g.GRNHeader.GRNNumber == td.BatchNumber).Select(g => g.ExpDate).FirstOrDefault(),
                        ManufacturingDate = _context.GRNDetails.Where(g => g.ProductId == td.ProductId && g.GRNHeader.GRNNumber == td.BatchNumber).Select(g => g.MfgDate).FirstOrDefault(),
                        BatchNumber = td.BatchNumber,
                        ReferenceNumber = td.StockTransferHeader.TransferNumber,
                        CurrentStock = item.AvailableStock
                    })
                    .ToListAsync();

                var history = grnHistory.Concat(transferInHistory).Concat(transferOutHistory)
                    .OrderBy(h => h.ReceivedDate)
                    .ToList();

                // EXACT BATCH MATCHING for Sales
                var exactBatchSoldMap = itemTransactions
                    .Where(tx => tx.TransactionType.Contains("Sale") && !tx.TransactionType.Contains("REVERSED") && !string.IsNullOrEmpty(tx.BatchNumber))
                    .GroupBy(tx => tx.BatchNumber)
                    .ToDictionary(g => g.Key, g => g.Sum(tx => -tx.Quantity)); // Sale quantity is negative in InventoryTransaction, so -tx.Quantity gives positive sold amount

                var exactBatchSaleReversals = itemTransactions
                    .Where(tx => tx.TransactionType.Contains("Sale") && tx.TransactionType.Contains("REVERSED") && !string.IsNullOrEmpty(tx.BatchNumber))
                    .GroupBy(tx => tx.BatchNumber)
                    .ToDictionary(g => g.Key, g => g.Sum(tx => tx.Quantity)); // Reversal is positive

                // Calculate Net Exact Sold per Batch
                var netExactBatchSold = new Dictionary<string, decimal>();
                foreach (var kvp in exactBatchSoldMap)
                {
                    decimal sold = kvp.Value;
                    decimal reversed = exactBatchSaleReversals.ContainsKey(kvp.Key) ? exactBatchSaleReversals[kvp.Key] : 0;
                    decimal netSold = sold - reversed;
                    if (netSold > 0)
                    {
                        netExactBatchSold[kvp.Key] = netSold;
                    }
                }

                // Calculate how much we need to distribute overall
                decimal totalSoldToDistribute = item.TotalSold;
                decimal remainingReturn = totalDeductibleReturn;
                decimal remainingTransfer = item.TotalTransferredOut;

                // PASS 1: Exact matches and Returns
                foreach (var h in history)
                {
                    h.AvailableQty = h.ReceivedQty - h.RejectedQty;
                    
                    // Deduct Purchase Returns (FIFO)
                    if (remainingReturn > 0 && h.AvailableQty > 0)
                    {
                        decimal toReturn = Math.Min(remainingReturn, h.AvailableQty);
                        h.ReturnedQty = toReturn;
                        h.AvailableQty -= toReturn;
                        remainingReturn -= toReturn;
                    }

                    // Deduct Exact Sales for this Batch
                    if (h.BatchNumber != null && netExactBatchSold.ContainsKey(h.BatchNumber) && h.AvailableQty > 0)
                    {
                        decimal exactSold = netExactBatchSold[h.BatchNumber];
                        decimal toExactSold = Math.Min(exactSold, h.AvailableQty);
                        h.SoldQty += toExactSold;
                        h.AvailableQty -= toExactSold;
                        netExactBatchSold[h.BatchNumber] -= toExactSold;
                        totalSoldToDistribute -= toExactSold;
                    }
                }

                // PASS 2: Distribute leftover sales and transfers (FIFO)
                foreach (var h in history)
                {
                    if (totalSoldToDistribute > 0 && h.AvailableQty > 0)
                    {
                        decimal toSold = Math.Min(totalSoldToDistribute, h.AvailableQty);
                        h.SoldQty += toSold;
                        h.AvailableQty -= toSold;
                        totalSoldToDistribute -= toSold;
                    }

                    if (remainingTransfer > 0 && h.AvailableQty > 0)
                    {
                        decimal toTransfer = Math.Min(remainingTransfer, h.AvailableQty);
                        h.TransferredQty = toTransfer;
                        h.AvailableQty -= toTransfer;
                        remainingTransfer -= toTransfer;
                    }

                    if (h.IsAlreadyPurged)
                    {
                        var batchPurged = itemTransactions
                            .Where(tx => tx.TransactionType == "StockPurge-OUT" && tx.ExpDate?.Date == h.ExpiryDate?.Date)
                            .Sum(tx => Math.Abs(tx.Quantity));
                        h.ExpiredQty = batchPurged;
                        h.ReceivedQty = batchPurged;
                    }
                }
                item.History = history;
            }

            return new StockPagedResponseDto
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<StockPagedResponseDto> GetDisposedStockAsync(string? search, string? sortField, string? sortOrder, int pageIndex, int pageSize, DateTime? startDate, DateTime? endDate, Guid? warehouseId = null, Guid? rackId = null, string? branchId = null)
        {
            // Disposed stock is essentially current stock with showPurged=true
            // but we filter it to only show items that have actual rejected or expired quantities
            var stockData = await GetCurrentStockAsync(search, sortField, sortOrder, 0, 1000, startDate, endDate, warehouseId, rackId, true, branchId);
            
            var disposedItems = stockData.Items
                .Where(x => x.TotalRejected > 0 || x.TotalExpired > 0)
                .ToList();

            var totalCount = disposedItems.Count;
            var pagedItems = disposedItems.Skip(pageIndex * pageSize).Take(pageSize).ToList();

            return new StockPagedResponseDto 
            { 
                Items = pagedItems, 
                TotalCount = totalCount 
            };
        }

        public async Task<byte[]> GenerateStockExcel(List<Guid> productIds)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var query = _context.WarehouseStocks
                .Include(ws => ws.Product)
                .Include(ws => ws.Warehouse)
                .Where(ws => productIds.Contains(ws.ProductId));

            if (!_currentUserService.IsPlatformAdmin)
            {
                query = query.Where(ws => ws.CompanyId == companyId);
            }

            var data = await query
                .Select(ws => new
                {
                    ws.Product.Name,
                    ws.Product.Sku,
                    WarehouseName = ws.Warehouse.Name,
                    ws.Product.Unit,
                    ws.Quantity,
                    ws.MinStock
                })
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Warehouse Stock");
                worksheet.Cell(1, 1).Value = "Product Name";
                worksheet.Cell(1, 2).Value = "SKU";
                worksheet.Cell(1, 3).Value = "Warehouse";
                worksheet.Cell(1, 4).Value = "Unit";
                worksheet.Cell(1, 5).Value = "Available Stock";
                worksheet.Cell(1, 6).Value = "Min Stock";
                worksheet.Cell(1, 7).Value = "Status";

                var range = worksheet.Range(1, 1, 1, 7);
                range.Style.Font.Bold = true;
                range.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5");
                range.Style.Font.FontColor = XLColor.White;

                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];
                    int row = i + 2;
                    worksheet.Cell(row, 1).Value = item.Name;
                    worksheet.Cell(row, 2).Value = item.Sku;
                    worksheet.Cell(row, 3).Value = item.WarehouseName;
                    worksheet.Cell(row, 4).Value = item.Unit;
                    worksheet.Cell(row, 5).Value = item.Quantity;
                    worksheet.Cell(row, 6).Value = item.MinStock;
                    worksheet.Cell(row, 7).Value = item.Quantity <= item.MinStock ? "Low Stock" : "Optimal";
                    
                    if (item.Quantity <= item.MinStock)
                        worksheet.Cell(row, 7).Style.Font.FontColor = XLColor.Red;
                }

                worksheet.Columns().AdjustToContents();
                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }


        public async Task<object> GetWarehouseStockAsync(string? search, string? sortField, string? sortOrder, int pageIndex, int pageSize, Guid? productId = null, Guid? warehouseId = null, string? branchId = null)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var finalBranchId = !string.IsNullOrEmpty(branchId) ? branchId : _currentUserService.BranchId;

            var query = _context.WarehouseStocks
                .IgnoreQueryFilters()
                .Include(ws => ws.Product)
                .Include(ws => ws.Warehouse)
                .AsQueryable();

            if (!_currentUserService.IsPlatformAdmin)
            {
                query = query.Where(ws => ws.CompanyId == companyId);
            }

            if (productId.HasValue && productId != Guid.Empty)
            {
                query = query.Where(ws => ws.ProductId == productId);
            }

            if (warehouseId.HasValue && warehouseId != Guid.Empty)
            {
                query = query.Where(ws => ws.WarehouseId == warehouseId);
            }

            if (!_currentUserService.IsPlatformAdmin && !string.IsNullOrEmpty(finalBranchId))
            {
                query = query.Where(ws => ws.BranchId == finalBranchId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(ws => ws.Product.Name.ToLower().Contains(search) || 
                                         ws.Warehouse.Name.ToLower().Contains(search) ||
                                         (ws.Product.Sku != null && ws.Product.Sku.ToLower().Contains(search)));
            }

            // Sorting
            if (!string.IsNullOrWhiteSpace(sortField))
            {
                bool isDesc = sortOrder?.ToLower() == "desc";
                query = sortField.ToLower() switch
                {
                    "productname" => isDesc ? query.OrderByDescending(x => x.Product.Name) : query.OrderBy(x => x.Product.Name),
                    "warehousename" => isDesc ? query.OrderByDescending(x => x.Warehouse.Name) : query.OrderBy(x => x.Warehouse.Name),
                    "quantity" => isDesc ? query.OrderByDescending(x => x.Quantity) : query.OrderBy(x => x.Quantity),
                    _ => isDesc ? query.OrderByDescending(x => x.CreatedOn) : query.OrderBy(x => x.CreatedOn)
                };
            }
            else
            {
                query = query.OrderBy(x => x.Warehouse.Name).ThenBy(x => x.Product.Name);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .Select(ws => new
                {
                    ws.Id,
                    ws.ProductId,
                    ProductName = ws.Product.Name,
                    ws.Product.Sku,
                    ws.Product.Unit,
                    ws.WarehouseId,
                    WarehouseName = ws.Warehouse.Name,
                    ws.Quantity,
                    ws.MinStock,
                    IsLowStock = ws.Quantity <= ws.MinStock,
                    BranchId = ws.BranchId,
                    CompanyId = ws.CompanyId
                })
                .ToListAsync();

            return new { items, totalCount };
        }

        public async Task<List<BatchTransactionDto>> GetBatchTransactionsAsync(Guid productId, Guid warehouseId, Guid rackId, DateTime? mfgDate, DateTime? expDate, string? branchId = null)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var finalBranchId = !string.IsNullOrEmpty(branchId) ? branchId : _currentUserService.BranchId;

            var query = _context.InventoryTransactions
                .Include(tx => tx.Product)
                .Include(tx => tx.Warehouse)
                .Include(tx => tx.Rack)
                .Where(tx => tx.ProductId == productId && tx.WarehouseId == warehouseId && tx.RackId == rackId);

            if (!_currentUserService.IsPlatformAdmin)
            {
                query = query.Where(tx => tx.CompanyId == companyId);
                if (!string.IsNullOrEmpty(finalBranchId))
                {
                    query = query.Where(tx => tx.BranchId == finalBranchId);
                }
            }

            if (mfgDate.HasValue) query = query.Where(tx => tx.MfgDate.HasValue && tx.MfgDate.Value.Date == mfgDate.Value.Date);
            if (expDate.HasValue) query = query.Where(tx => tx.ExpDate.HasValue && tx.ExpDate.Value.Date == expDate.Value.Date);

            return await query
                .OrderByDescending(tx => tx.TransactionDate)
                .Select(tx => new BatchTransactionDto
                {
                    TransactionDate = tx.TransactionDate,
                    TransactionType = tx.TransactionType,
                    Quantity = tx.Quantity,
                    RemainingStock = 0, // Placeholder
                    ReferenceNumber = tx.ReferenceNumber,
                    BatchNumber = tx.BatchNumber,
                    ManufacturingDate = tx.MfgDate,
                    ExpiryDate = tx.ExpDate,
                    WarehouseName = tx.Warehouse.Name,
                    RackName = tx.Rack.Name,
                    BranchId = tx.BranchId
                })
                .ToListAsync();
        }
    }
}
