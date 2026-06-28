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
            var isGlobalAdmin = _currentUserService.IsPlatformAdmin && companyId == Guid.Empty;

            // STEP 1: Base Query - Start from Products to ensure all items are included
            var baseQuery = _context.Products.IgnoreQueryFilters().Include(p => p.Category).AsNoTracking().AsQueryable();

            if (!isGlobalAdmin)
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
                .GroupJoin(_context.Warehouses.IgnoreQueryFilters().AsNoTracking(),
                    x => x.ri.WarehouseId, w => w.Id, (x, warehouses) => new { x.ri, x.p, warehouses })
                .SelectMany(x => x.warehouses.DefaultIfEmpty(), (x, w) => new { x.ri, x.p, w })
                .GroupJoin(_context.Racks.IgnoreQueryFilters().AsNoTracking(),
                    x => x.ri.RackId, r => r.Id, (x, racks) => new { x.ri, x.p, x.w, racks })
                .SelectMany(x => x.racks.DefaultIfEmpty(), (x, r) => new { x.ri, x.p, x.w, r })
                .Select(x => new
                {
                    ProductId = x.p.Id,
                    ProductName = x.p.Name,
                    CategoryName = x.p.Category != null ? x.p.Category.CategoryName : "N/A",
                    UnitName = x.p.Unit,
                    MinStock = x.p.MinStock,
                    WarehouseId = x.ri.WarehouseId,
                    WarehouseName = x.w != null ? x.w.Name : "N/A",
                    RackId = x.ri.RackId,
                    RackName = x.r != null ? x.r.Name : "N/A",
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

            if (!isGlobalAdmin && !string.IsNullOrEmpty(finalBranchId))
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
                        x.GrnRackName.ToLower().Contains("expired") || 
                        x.GrnRackName.ToLower().Contains("damaged") || 
                        x.GrnRackName.ToLower().Contains("rejected") || 
                        x.GrnRackName.ToLower().Contains("purged") || 
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
                        x.GrnRackName.ToLower().Contains("expired") || 
                        x.GrnRackName.ToLower().Contains("damaged") || 
                        x.GrnRackName.ToLower().Contains("rejected") || 
                        x.GrnRackName.ToLower().Contains("purged") || 
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

            // PRE-FETCH BULK AGGREGATES TO ELIMINATE N+1 QUERIES
            var salesGroup = await _context.SaleOrderItems.IgnoreQueryFilters().AsNoTracking()
                .Where(si => si.CompanyId == companyId 
                    && (string.IsNullOrEmpty(finalBranchId) || si.BranchId == finalBranchId)
                    && si.SaleOrder.Status != "Draft" && si.SaleOrder.Status != "Cancelled" && si.SaleOrder.Status != "Canceled")
                .GroupBy(si => new { si.ProductId, si.WarehouseId, si.RackId })
                .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, g.Key.RackId, Qty = g.Sum(x => (decimal?)x.Qty) ?? 0M })
                .ToListAsync();

            var salesLookup = salesGroup.ToDictionary(
                x => (x.ProductId, (Guid?)x.WarehouseId, (Guid?)x.RackId),
                x => x.Qty
            );

            var unlinkedSalesLookup = salesGroup
                .Where(x => x.WarehouseId == null || x.RackId == null)
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var invoicesGroup = await _context.SalesInvoiceItems.IgnoreQueryFilters().AsNoTracking()
                .Where(ii => ii.CompanyId == companyId 
                    && (string.IsNullOrEmpty(finalBranchId) || ii.BranchId == finalBranchId)
                    && ii.SalesInvoice.Status != "Draft" && ii.SalesInvoice.Status != "Cancelled" && ii.SalesInvoice.Status != "Canceled")
                .GroupBy(ii => new { ii.ProductId, ii.WarehouseId, ii.RackId })
                .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, g.Key.RackId, Qty = g.Sum(x => (decimal?)x.Qty) ?? 0M })
                .ToListAsync();

            var invoicesLookup = invoicesGroup.ToDictionary(
                x => (x.ProductId, (Guid?)x.WarehouseId, (Guid?)x.RackId),
                x => x.Qty
            );

            var unlinkedInvoicesLookup = invoicesGroup
                .Where(x => x.WarehouseId == null || x.RackId == null)
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var returnsGroup = await _context.SaleReturnItems.IgnoreQueryFilters().AsNoTracking()
                .Where(sri => sri.CompanyId == companyId 
                    && (string.IsNullOrEmpty(finalBranchId) || sri.BranchId == finalBranchId)
                    && (sri.SaleReturnHeader.Status == "Confirmed" || sri.SaleReturnHeader.Status == "INWARDED" || sri.SaleReturnHeader.Status == "Refunded" || sri.SaleReturnHeader.Status == "Exchanged"))
                .GroupBy(sri => new { sri.ProductId, sri.WarehouseId, sri.RackId })
                .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, g.Key.RackId, Qty = g.Sum(x => (decimal?)x.ReturnQty) ?? 0M })
                .ToListAsync();

            var returnsLookup = returnsGroup.ToDictionary(
                x => (x.ProductId, (Guid?)x.WarehouseId, (Guid?)x.RackId),
                x => x.Qty
            );

            var exchangeGroup = await _context.SaleExchangeItems.IgnoreQueryFilters().AsNoTracking()
                .Where(sei => sei.CompanyId == companyId 
                    && (string.IsNullOrEmpty(finalBranchId) || sei.BranchId == finalBranchId)
                    && (sei.SaleReturnHeader.Status == "Confirmed" || sei.SaleReturnHeader.Status == "INWARDED" || sei.SaleReturnHeader.Status == "Refunded" || sei.SaleReturnHeader.Status == "Exchanged"))
                .GroupBy(sei => new { sei.ProductId, sei.WarehouseId, sei.RackId })
                .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, g.Key.RackId, Qty = g.Sum(x => (decimal?)x.Qty) ?? 0M })
                .ToListAsync();

            var exchangeLookup = exchangeGroup.ToDictionary(
                x => (x.ProductId, (Guid?)x.WarehouseId, (Guid?)x.RackId),
                x => x.Qty
            );

            var prItemsGroup = await _context.PurchaseReturnItems.IgnoreQueryFilters().AsNoTracking()
                .Where(pri => pri.CompanyId == companyId 
                    && pri.PurchaseReturn.Status != "Cancelled" && pri.PurchaseReturn.Status != "Canceled")
                .Select(pri => new { pri.ProductId, pri.WarehouseId, pri.RackId, ReturnQty = pri.ReturnQty, pri.GrnRef })
                .ToListAsync();

            var prItemsLookup = prItemsGroup
                .GroupBy(x => (x.ProductId, (Guid?)x.WarehouseId, (Guid?)x.RackId))
                .ToDictionary(g => g.Key, g => g.ToList());

            var grnDetailsQuery = _context.GRNDetails.IgnoreQueryFilters().AsNoTracking()
                .Where(g => g.CompanyId == companyId && g.GRNHeader.Status != "Cancelled");

            if (!isGlobalAdmin && !string.IsNullOrEmpty(finalBranchId))
            {
                grnDetailsQuery = grnDetailsQuery.Where(g => g.BranchId == finalBranchId);
            }

            var grnDetailsList = await grnDetailsQuery
                .Select(g => new
                {
                    g.Id,
                    g.ProductId,
                    g.WarehouseId,
                    g.RackId,
                    g.ReceivedQty,
                    g.RejectedQty,
                    g.MfgDate,
                    g.ExpDate,
                    ReceivedDate = g.GRNHeader.ReceivedDate,
                    BatchNumber = g.GRNHeader.GRNNumber,
                    GrnUnitRate = g.UnitRate,
                    GrnPurchaseOrderId = (Guid?)g.GRNHeader.PurchaseOrderId,
                    GrnBatchNumber = g.BatchNumber,
                    PONumber = g.GRNHeader.PurchaseOrder.PoNumber,
                    SupplierName = g.GRNHeader.PurchaseOrder.SupplierName ?? "N/A",
                    IsQuick = g.GRNHeader.IsQuick,
                    RackName = g.Rack != null ? g.Rack.Name : "N/A",
                    RackDescription = g.Rack != null ? g.Rack.Description : "N/A",
                    WarehouseName = g.Warehouse != null ? g.Warehouse.Name : "N/A",
                    ReferenceNumber = g.ReferenceNumber ?? g.GRNHeader.PurchaseOrder.PoNumber,
                    g.BranchId
                })
                .ToListAsync();

            var grnLookup = grnDetailsList
                .GroupBy(x => (x.ProductId, (Guid?)x.WarehouseId, (Guid?)x.RackId))
                .ToDictionary(g => g.Key, g => g.ToList());

            var grnByNumberLookup = grnDetailsList
                .GroupBy(x => x.BatchNumber)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault());

            var oldestGrnLookup = grnDetailsList
                .GroupBy(g => g.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.ReceivedDate).Select(x => new { x.WarehouseId, x.RackId }).FirstOrDefault()
                );

            var transactionsList = await _context.InventoryTransactions.IgnoreQueryFilters().AsNoTracking()
                .Where(tx => tx.CompanyId == companyId)
                .Select(tx => new { tx.ProductId, tx.WarehouseId, tx.RackId, tx.Quantity, tx.TransactionType, tx.ExpDate, tx.BatchNumber })
                .ToListAsync();

            var transactionsLookup = transactionsList
                .GroupBy(x => (x.ProductId, (Guid?)x.WarehouseId, (Guid?)x.RackId))
                .ToDictionary(g => g.Key, g => g.ToList());

            var transfersOutList = await _context.StockTransferDetails.IgnoreQueryFilters().AsNoTracking()
                .Where(td => td.CompanyId == companyId && (td.StockTransferHeader.Status == "Dispatched" || td.StockTransferHeader.Status == "Completed"))
                .Select(td => new 
                { 
                    td.ProductId, 
                    td.Quantity, 
                    td.BatchNumber, 
                    FromWarehouseId = td.StockTransferHeader.FromWarehouseId, 
                    ToBranchId = td.StockTransferHeader.ToBranchId,
                    TransferDate = td.StockTransferHeader.TransferDate,
                    TransferNumber = td.StockTransferHeader.TransferNumber,
                    SourceWarehouseName = td.StockTransferHeader.FromWarehouse.Name,
                    DestinationWarehouseName = td.StockTransferHeader.ToWarehouse.Name,
                    SourceBranchId = td.StockTransferHeader.FromWarehouse.BranchId
                })
                .ToListAsync();

            var transfersOutLookup = transfersOutList
                .GroupBy(x => (x.ProductId, (Guid?)x.FromWarehouseId))
                .ToDictionary(g => g.Key, g => g.ToList());

            var transfersInList = await _context.StockTransferDetails.IgnoreQueryFilters().AsNoTracking()
                .Where(td => td.CompanyId == companyId && td.StockTransferHeader.Status == "Completed")
                .Select(td => new 
                { 
                    td.ProductId, 
                    td.Quantity, 
                    td.BatchNumber, 
                    ToWarehouseId = td.StockTransferHeader.ToWarehouseId, 
                    SourceWarehouseName = td.StockTransferHeader.FromWarehouse.Name, 
                    TransferNumber = td.StockTransferHeader.TransferNumber, 
                    TransferDate = td.StockTransferHeader.TransferDate, 
                    ToBranchId = td.StockTransferHeader.ToBranchId, 
                    FromBranchId = td.StockTransferHeader.FromBranchId 
                })
                .ToListAsync();

            var transfersInLookup = transfersInList
                .GroupBy(x => (x.ProductId, (Guid?)x.ToWarehouseId))
                .ToDictionary(g => g.Key, g => g.ToList());

            var challansGroup = await _context.DeliveryChallanItems.IgnoreQueryFilters().AsNoTracking()
                .Where(dci => dci.CompanyId == companyId && (string.IsNullOrEmpty(finalBranchId) || dci.BranchId == finalBranchId)
                    && (dci.DeliveryChallan.Status == "Pending" || dci.DeliveryChallan.Status == "Draft"))
                .GroupBy(dci => new { dci.ProductId, dci.WarehouseId, dci.RackId })
                .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, g.Key.RackId, Qty = g.Sum(x => (decimal?)x.Qty) ?? 0M })
                .ToListAsync();

            var challansLookup = challansGroup.ToDictionary(
                x => (x.ProductId, (Guid?)x.WarehouseId, (Guid?)x.RackId),
                x => x.Qty
            );

            // STEP 3: In-Memory Calculations
            foreach (var item in items)
            {
                var lookupKey = (item.ProductId, item.WarehouseId, item.RackId);

                var grossSold = salesLookup.GetValueOrDefault(lookupKey, 0M);
                var quickSold = invoicesLookup.GetValueOrDefault(lookupKey, 0M);
                grossSold += quickSold;

                var totalSaleReturn = returnsLookup.GetValueOrDefault(lookupKey, 0M);
                var totalExchanged = exchangeLookup.GetValueOrDefault(lookupKey, 0M);

                var purchaseReturns = prItemsLookup.GetValueOrDefault(lookupKey);
                decimal totalPurchaseReturn = 0;
                decimal totalDeductibleReturn = 0;

                if (purchaseReturns != null)
                {
                    foreach (var pr in purchaseReturns)
                    {
                        totalPurchaseReturn += pr.ReturnQty;
                        var gd = grnByNumberLookup.GetValueOrDefault(pr.GrnRef);
                        decimal rejected = gd?.RejectedQty ?? 0M;
                        totalDeductibleReturn += Math.Max(0M, pr.ReturnQty - rejected);
                    }
                }

                var itemTransactions = transactionsLookup.GetValueOrDefault(lookupKey);
                decimal totalPurged = 0M;

                if (itemTransactions != null)
                {
                    totalPurged = Math.Abs(itemTransactions
                        .Where(tx => tx.TransactionType == "StockPurge-OUT")
                        .Sum(tx => (decimal)tx.Quantity));
                }

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

                // TRANSFER OUT
                var currentTransfersOut = transfersOutLookup.GetValueOrDefault((item.ProductId, item.WarehouseId));
                var curGrns = grnLookup.GetValueOrDefault(lookupKey);

                decimal transferredOut = 0M;
                if (currentTransfersOut != null)
                {
                    transferredOut = currentTransfersOut
                        .Where(td => curGrns != null && curGrns.Any(g => g.GrnBatchNumber == td.BatchNumber || g.BatchNumber == td.BatchNumber))
                        .Sum(td => (decimal)td.Quantity);
                }

                // TRANSFER IN
                var currentTransfersIn = transfersInLookup.GetValueOrDefault((item.ProductId, item.WarehouseId));
                decimal transferredIn = 0M;
                if (currentTransfersIn != null && item.RackId == null)
                {
                    transferredIn = currentTransfersIn.Sum(td => (decimal)td.Quantity);
                }

                var unlinkedSales = unlinkedSalesLookup.GetValueOrDefault(item.ProductId, 0M) + unlinkedInvoicesLookup.GetValueOrDefault(item.ProductId, 0M);
                var isOldest = oldestGrnLookup.GetValueOrDefault(item.ProductId);
                var adjustment = (isOldest != null && isOldest.WarehouseId == item.WarehouseId && isOldest.RackId == item.RackId) ? unlinkedSales : 0M;

                var pendingChallanQty = challansLookup.GetValueOrDefault(lookupKey, 0M);

                item.TotalSold = grossSold + adjustment - totalSaleReturn + pendingChallanQty + totalExchanged;
                item.TotalReturned = totalPurchaseReturn;
                item.TotalTransferredOut = transferredOut;
                item.TotalTransferredIn = transferredIn;
                if (transferredOut > 0 && currentTransfersOut != null)
                {
                    item.TransferredBranchId = currentTransfersOut
                        .Select(td => (string)td.ToBranchId)
                        .FirstOrDefault();
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

                var earliestBatch = curGrns != null ? curGrns
                    .OrderBy(g => g.ExpDate ?? DateTime.MaxValue)
                    .ThenBy(g => g.ReceivedDate)
                    .Select(g => new { g.MfgDate, g.ExpDate, g.ReceivedDate, BatchNumber = g.BatchNumber })
                    .FirstOrDefault() : null;

                if (earliestBatch != null)
                {
                    item.ManufacturingDate = earliestBatch.MfgDate;
                    item.ExpiryDate = earliestBatch.ExpDate;
                    item.ReceivedDate = earliestBatch.ReceivedDate;
                    item.BatchNumber = earliestBatch.BatchNumber;
                }
                else if (transferredIn > 0 && currentTransfersIn != null)
                {
                    var earliestTransfer = currentTransfersIn
                        .OrderBy(td => td.TransferDate)
                        .Select(td => new { td.BatchNumber, td.TransferDate })
                        .FirstOrDefault();

                    if (earliestTransfer != null)
                    {
                        var originalBatch = grnByNumberLookup.GetValueOrDefault(earliestTransfer.BatchNumber);
                        if (originalBatch != null)
                        {
                            item.ManufacturingDate = originalBatch.MfgDate;
                            item.ExpiryDate = originalBatch.ExpDate;
                        }
                        item.ReceivedDate = earliestTransfer.TransferDate;
                        item.BatchNumber = earliestTransfer.BatchNumber;
                    }
                }

                var grnHistory = curGrns != null ? curGrns.Select(allG => new StockHistoryDto
                {
                    ProductId = allG.ProductId,
                    WarehouseId = allG.WarehouseId,
                    RackId = allG.RackId,
                    ReceivedDate = allG.ReceivedDate,
                    PONumber = allG.PONumber,
                    GRNNumber = allG.BatchNumber,
                    SupplierName = allG.SupplierName,
                    TransactionType = allG.IsQuick ? "QuickGRN" : "GRN",
                    ProductName = item.ProductName,
                    ReceivedQty = allG.ReceivedQty,
                    ExpiredQty = (allG.RackName.ToLower().Contains("e1") || 
                                  allG.RackName.ToLower().Contains("expired") || 
                                  allG.RackName.ToLower().Contains("damaged") || 
                                  allG.RackName.ToLower().Contains("rejected") || 
                                  allG.RackName.ToLower().Contains("purged") || 
                                  allG.RackName.ToLower().StartsWith("e -") || 
                                  allG.RackName.ToLower().StartsWith("e-") ||
                                  (allG.RackDescription != null && (
                                      allG.RackDescription.ToLower().Contains("expired") || 
                                      allG.RackDescription.ToLower().Contains("damaged") || 
                                      allG.RackDescription.ToLower().Contains("rejected") ||
                                      allG.RackDescription.ToLower().Contains("purged")
                                  ))
                                 ) ? allG.RejectedQty : 0,
                    RejectedQty = (allG.RackName.ToLower().Contains("e1") || 
                                   allG.RackName.ToLower().Contains("expired") || 
                                   allG.RackName.ToLower().Contains("damaged") || 
                                   allG.RackName.ToLower().Contains("rejected") || 
                                   allG.RackName.ToLower().Contains("purged") || 
                                   allG.RackName.ToLower().StartsWith("e -") || 
                                   allG.RackName.ToLower().StartsWith("e-") ||
                                   (allG.RackDescription != null && (
                                       allG.RackDescription.ToLower().Contains("expired") || 
                                       allG.RackDescription.ToLower().Contains("damaged") || 
                                       allG.RackDescription.ToLower().Contains("rejected") ||
                                       allG.RackDescription.ToLower().Contains("purged")
                                   ))
                                  ) ? 0 : allG.RejectedQty,
                    ManufacturingDate = allG.MfgDate,
                    ExpiryDate = allG.ExpDate,
                    IsExpiryRequired = item.IsExpiryRequired,
                    WarehouseName = allG.WarehouseName,
                    RackName = allG.RackName,
                    AvailableQty = allG.ReceivedQty - allG.RejectedQty, 
                    CurrentStock = item.AvailableStock, 
                    TotalReturned = totalPurchaseReturn,
                    BranchId = allG.BranchId,
                    BranchName = allG.BranchId,
                    BatchNumber = allG.GrnBatchNumber,
                    ReferenceNumber = allG.ReferenceNumber,
                    IsAlreadyPurged = allG.ReceivedQty == 0 && allG.RejectedQty == 0
                }).ToList() : new List<StockHistoryDto>();

                var transferInHistory = (item.RackId == null && currentTransfersIn != null) ? currentTransfersIn.Select(td => new StockHistoryDto
                {
                    ProductId = td.ProductId,
                    WarehouseId = item.WarehouseId,
                    ReceivedDate = td.TransferDate,
                    PONumber = td.TransferNumber,
                    GRNNumber = "TRANSFER-IN",
                    SupplierName = "From: " + td.SourceWarehouseName,
                    TransactionType = "Transfer",
                    ProductName = item.ProductName,
                    ReceivedQty = td.Quantity,
                    TransferredQty = td.Quantity,
                    AvailableQty = td.Quantity,
                    WarehouseName = td.SourceWarehouseName,
                    RackName = "Transferred In",
                    BranchId = td.ToBranchId,
                    BranchName = td.ToBranchId,
                    TransferredFromBranchId = td.FromBranchId,
                    TransferredFromBranchName = td.FromBranchId,
                    IsExpiryRequired = item.IsExpiryRequired,
                    ExpiryDate = grnByNumberLookup.GetValueOrDefault(td.BatchNumber)?.ExpDate,
                    ManufacturingDate = grnByNumberLookup.GetValueOrDefault(td.BatchNumber)?.MfgDate,
                    BatchNumber = td.BatchNumber,
                    ReferenceNumber = td.TransferNumber,
                    CurrentStock = item.AvailableStock
                }).ToList() : new List<StockHistoryDto>();

                var transferOutHistory = (currentTransfersOut != null) ? currentTransfersOut
                    .Where(td => curGrns != null && curGrns.Any(g => g.GrnBatchNumber == td.BatchNumber || g.BatchNumber == td.BatchNumber))
                    .Select(td => new StockHistoryDto
                    {
                        ProductId = td.ProductId,
                        WarehouseId = item.WarehouseId,
                        ReceivedDate = td.TransferDate,
                        PONumber = td.TransferNumber,
                        GRNNumber = "TRANSFER-OUT",
                        SupplierName = "To: " + td.DestinationWarehouseName,
                        TransactionType = "Transfer",
                        ProductName = item.ProductName,
                        ReceivedQty = 0,
                        SoldQty = 0, 
                        TransferredQty = td.Quantity,
                        AvailableQty = -td.Quantity,
                        WarehouseName = item.WarehouseName,
                        RackName = item.RackName,
                        BranchId = td.SourceBranchId,
                        BranchName = td.SourceBranchId,
                        TransferredToBranchId = td.ToBranchId,
                        TransferredToBranchName = td.ToBranchId,
                        ExpiryDate = grnByNumberLookup.GetValueOrDefault(td.BatchNumber)?.ExpDate,
                        ManufacturingDate = grnByNumberLookup.GetValueOrDefault(td.BatchNumber)?.MfgDate,
                        BatchNumber = td.BatchNumber,
                        ReferenceNumber = td.TransferNumber,
                        CurrentStock = item.AvailableStock
                    }).ToList() : new List<StockHistoryDto>();

                var history = grnHistory.Concat(transferInHistory).Concat(transferOutHistory)
                    .OrderBy(h => h.ReceivedDate)
                    .ToList();

                var exactBatchSoldMap = itemTransactions != null ? itemTransactions
                    .Where(tx => tx.TransactionType.Contains("Sale") && !tx.TransactionType.Contains("REVERSED") && !string.IsNullOrEmpty(tx.BatchNumber))
                    .GroupBy(tx => tx.BatchNumber)
                    .ToDictionary(g => (string)g.Key, g => g.Sum(tx => -(decimal)tx.Quantity)) : new Dictionary<string, decimal>();

                var exactBatchSaleReversals = itemTransactions != null ? itemTransactions
                    .Where(tx => tx.TransactionType.Contains("Sale") && tx.TransactionType.Contains("REVERSED") && !string.IsNullOrEmpty(tx.BatchNumber))
                    .GroupBy(tx => tx.BatchNumber)
                    .ToDictionary(g => (string)g.Key, g => g.Sum(tx => (decimal)tx.Quantity)) : new Dictionary<string, decimal>();

                var netExactBatchSold = new Dictionary<string, decimal>();
                foreach (var kvp in exactBatchSoldMap)
                {
                    decimal sold = kvp.Value;
                    decimal reversed = exactBatchSaleReversals.ContainsKey(kvp.Key) ? exactBatchSaleReversals[kvp.Key] : 0M;
                    decimal netSold = sold - reversed;
                    if (netSold > 0)
                    {
                        netExactBatchSold[kvp.Key] = netSold;
                    }
                }

                decimal totalSoldToDistribute = item.TotalSold;
                decimal remainingReturn = totalDeductibleReturn;
                decimal remainingTransfer = item.TotalTransferredOut;

                foreach (var h in history)
                {
                    h.AvailableQty = h.ReceivedQty - h.RejectedQty;
                    
                    if (remainingReturn > 0 && h.AvailableQty > 0)
                    {
                        decimal toReturn = Math.Min(remainingReturn, h.AvailableQty);
                        h.ReturnedQty = toReturn;
                        h.AvailableQty -= toReturn;
                        remainingReturn -= toReturn;
                    }

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

                    if (h.IsAlreadyPurged && itemTransactions != null)
                    {
                        var batchPurged = itemTransactions
                            .Where(tx => tx.TransactionType == "StockPurge-OUT" && tx.ExpDate?.Date == h.ExpiryDate?.Date)
                            .Sum(tx => Math.Abs((decimal)tx.Quantity));
                        h.ExpiredQty = batchPurged;
                        h.ReceivedQty = batchPurged;
                    }
                }
                item.History = history;
            }            return new StockPagedResponseDto
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

            var isGlobalAdmin = _currentUserService.IsPlatformAdmin && companyId == Guid.Empty;
            if (!isGlobalAdmin)
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

            var isGlobalAdmin = _currentUserService.IsPlatformAdmin && companyId == Guid.Empty;
            if (!isGlobalAdmin)
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

            if (!isGlobalAdmin && !string.IsNullOrEmpty(finalBranchId))
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

            var isGlobalAdmin = _currentUserService.IsPlatformAdmin && companyId == Guid.Empty;
            if (!isGlobalAdmin)
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
                .OrderByDescending(tx => tx.CreatedOn)
                .Select(tx => new BatchTransactionDto
                {
                    TransactionDate = tx.CreatedOn ?? DateTime.Now,
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
