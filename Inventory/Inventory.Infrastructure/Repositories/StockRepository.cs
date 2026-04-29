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
            var baseQuery = _context.Products.AsNoTracking()
                .Where(p => p.CompanyId == companyId)
                .GroupJoin(_context.GRNDetails.AsNoTracking(), p => p.Id, g => g.ProductId, (p, g) => new { p, g })
                .SelectMany(x => x.g.DefaultIfEmpty(), (x, g) => new 
                { 
                    Product = x.p, 
                    GRN = g,
                    ProductId = x.p.Id,
                    WarehouseId = g != null ? (Guid?)g.WarehouseId : null,
                    RackId = g != null ? (Guid?)g.RackId : null,
                    BranchId = g != null ? g.BranchId : x.p.BranchId,
                    ReceivedQty = g != null ? g.ReceivedQty : 0,
                    RejectedQty = g != null ? g.RejectedQty : 0,
                    ModifiedOn = g != null ? g.ModifiedOn : x.p.CreatedOn,
                    GRNId = g != null ? (Guid?)g.Id : null
                });

            if (!string.IsNullOrEmpty(finalBranchId))
            {
                baseQuery = baseQuery.Where(x => x.BranchId == finalBranchId || x.BranchId == null);
            }

            if (startDate.HasValue)
                baseQuery = baseQuery.Where(x => x.GRN != null && x.GRN.GRNHeader.ReceivedDate.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                baseQuery = baseQuery.Where(x => x.GRN != null && x.GRN.GRNHeader.ReceivedDate.Date <= endDate.Value.Date);

            if (warehouseId.HasValue && warehouseId.Value != Guid.Empty)
                baseQuery = baseQuery.Where(x => x.WarehouseId == warehouseId.Value);
            if (rackId.HasValue && rackId.Value != Guid.Empty)
                baseQuery = baseQuery.Where(x => x.RackId == rackId.Value);

            // STEP 2: Grouping Logic
            var groupedQuery = baseQuery
                .GroupBy(g => new
                {
                    g.ProductId,
                    ProductName = g.Product.Name,
                    UnitName = g.Product.Unit,
                    MinStock = g.Product.MinStock,
                    g.WarehouseId,
                    WarehouseName = (g.GRN != null && g.GRN.Warehouse != null) ? g.GRN.Warehouse.Name : "N/A",
                    g.RackId,
                    RackName = (g.GRN != null && g.GRN.Rack != null) ? g.GRN.Rack.Name : "N/A",
                    Sku = g.Product.Sku,
                    GstPercent = g.Product.DefaultGst ?? 0,
                    IsExpiryRequired = g.Product.IsExpiryRequired,
                    BranchId = g.BranchId
                })
                .Select(group => new StockSummaryDto
                {
                    ProductId = group.Key.ProductId,
                    ProductName = group.Key.ProductName,
                    Unit = group.Key.UnitName,
                    MinStockLevel = group.Key.MinStock,
                    WarehouseId = group.Key.WarehouseId,
                    WarehouseName = group.Key.WarehouseName,
                    RackId = group.Key.RackId,
                    RackName = group.Key.RackName,
                    Sku = group.Key.Sku,
                    GstPercent = group.Key.GstPercent,
                    IsExpiryRequired = group.Key.IsExpiryRequired,
                    BranchId = group.Key.BranchId,
                    TotalReceived = group.Sum(x => x.ReceivedQty),
                    TotalRejected = group.Sum(x => (x.GRN != null && x.GRN.Rack != null && (
                        x.GRN.Rack.Name.ToLower().Contains("e1") || 
                        x.GRN.Rack.Name.ToLower().StartsWith("e -") || 
                        x.GRN.Rack.Name.ToLower().StartsWith("e-") ||
                        (x.GRN.Rack.Description != null && (
                            x.GRN.Rack.Description.ToLower().Contains("expired") || 
                            x.GRN.Rack.Description.ToLower().Contains("damaged") || 
                            x.GRN.Rack.Description.ToLower().Contains("rejected") ||
                            x.GRN.Rack.Description.ToLower().Contains("purged")
                        ))
                    )) ? 0 : x.RejectedQty),
                    TotalExpired = group.Sum(x => (x.GRN != null && x.GRN.Rack != null && (
                        x.GRN.Rack.Name.ToLower().Contains("e1") || 
                        x.GRN.Rack.Name.ToLower().StartsWith("e -") || 
                        x.GRN.Rack.Name.ToLower().StartsWith("e-") ||
                        (x.GRN.Rack.Description != null && (
                            x.GRN.Rack.Description.ToLower().Contains("expired") || 
                            x.GRN.Rack.Description.ToLower().Contains("damaged") || 
                            x.GRN.Rack.Description.ToLower().Contains("rejected") ||
                            x.GRN.Rack.Description.ToLower().Contains("purged")
                        ))
                    )) ? x.RejectedQty : 0),
                    AvailableStock = group.Sum(x => x.ReceivedQty - x.RejectedQty),
                    IsAlreadyPurged = group.Sum(x => x.ReceivedQty) == 0 && group.Sum(x => x.RejectedQty) == 0,
                    PurgedDate = (group.Sum(x => x.ReceivedQty) == 0 && group.Sum(x => x.RejectedQty) == 0) ? group.Max(x => x.ModifiedOn) : null,
                    LastRate = group.OrderByDescending(x => x.GRNId).Select(x => x.GRN != null ? x.GRN.UnitRate : 0).FirstOrDefault(),
                    LastPurchaseOrderId = group.OrderByDescending(x => x.GRNId).Select(x => (x.GRN != null && x.GRN.GRNHeader != null) ? x.GRN.GRNHeader.PurchaseOrderId : (Guid?)null).FirstOrDefault()
                });

            if (!showPurged)
            {
                groupedQuery = groupedQuery.Where(x => x.TotalReceived > 0 || x.TotalRejected > 0);
            }
            else
            {
                groupedQuery = groupedQuery.Where(x => 
                    x.TotalReceived > 0 || 
                    x.TotalRejected > 0 || 
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
                    groupedQuery = groupedQuery.Where(x => x.ProductName.Contains(search));
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
                var salesQuery = _context.SaleOrderItems.Where(si => si.CompanyId == companyId && si.ProductId == item.ProductId);
                var returnsQuery = _context.SaleReturnItems.Where(sri => sri.CompanyId == companyId && sri.ProductId == item.ProductId);
                var grnQuery = _context.GRNDetails.Where(g => g.CompanyId == companyId && g.ProductId == item.ProductId);

                if (!string.IsNullOrEmpty(branchId))
                {
                    salesQuery = salesQuery.Where(si => si.BranchId == branchId);
                    returnsQuery = returnsQuery.Where(sri => sri.BranchId == branchId);
                    grnQuery = grnQuery.Where(g => g.BranchId == branchId);
                }

                var grossSold = await salesQuery
                    .Where(si => si.WarehouseId == item.WarehouseId && si.RackId == item.RackId && (si.SaleOrder.Status == "Confirmed" || si.SaleOrder.Status == "Delivered" || si.SaleOrder.Status == "Completed"))
                    .SumAsync(si => (decimal?)si.Qty) ?? 0;

                var totalSaleReturn = await returnsQuery
                    .Where(sri => sri.WarehouseId == item.WarehouseId && sri.RackId == item.RackId && (sri.SaleReturnHeader.Status == "Confirmed" || sri.SaleReturnHeader.Status == "INWARDED"))
                    .SumAsync(sri => (decimal?)sri.ReturnQty) ?? 0;

                var totalPurchaseReturn = await _context.PurchaseReturnItems
                    .Where(pri => pri.CompanyId == companyId && pri.ProductId == item.ProductId && pri.WarehouseId == item.WarehouseId && pri.RackId == item.RackId)
                    .SumAsync(pri => (decimal?)pri.ReturnQty) ?? 0;

                // 🚀 TRANSFER CALCULATION
                var transferredOut = await _context.StockTransferDetails
                    .Where(td => td.CompanyId == companyId && td.ProductId == item.ProductId && td.StockTransferHeader.FromWarehouseId == item.WarehouseId)
                    .SumAsync(td => (decimal?)td.Quantity) ?? 0;

                var transferredIn = await _context.StockTransferDetails
                    .Where(td => td.CompanyId == companyId && td.ProductId == item.ProductId && td.StockTransferHeader.ToWarehouseId == item.WarehouseId)
                    .SumAsync(td => (decimal?)td.Quantity) ?? 0;

                var unlinkedSales = await salesQuery
                    .Where(si => (si.WarehouseId == null || si.RackId == null) && (si.SaleOrder.Status == "Confirmed" || si.SaleOrder.Status == "Delivered" || si.SaleOrder.Status == "Completed"))
                    .SumAsync(si => (decimal?)si.Qty) ?? 0;

                var isOldest = await grnQuery
                    .OrderBy(g => g.GRNHeader.ReceivedDate)
                    .Select(g => new { g.WarehouseId, g.RackId })
                    .FirstOrDefaultAsync();

                var adjustment = (isOldest != null && isOldest.WarehouseId == item.WarehouseId && isOldest.RackId == item.RackId) ? unlinkedSales : 0;

                item.TotalSold = grossSold + adjustment - totalSaleReturn;
                item.TotalReturned = totalPurchaseReturn;
                item.AvailableStock = item.TotalReceived - item.TotalRejected - item.TotalSold - totalPurchaseReturn - transferredOut + transferredIn;

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
                        IsAlreadyPurged = allG.ReceivedQty == 0 && allG.RejectedQty == 0
                    })
                    .ToListAsync();

                // 🚀 ADD TRANSFERS TO HISTORY
                var transferInHistory = await _context.StockTransferDetails
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
                        AvailableQty = td.Quantity,
                        WarehouseName = td.StockTransferHeader.FromWarehouse.Name, // 🎯 Source Warehouse
                        RackName = "Transferred In",
                        BranchId = td.StockTransferHeader.FromWarehouse.BranchId, // 🎯 Source Branch ID
                        BranchName = td.StockTransferHeader.FromWarehouse.BranchId, // 🎯 Source Branch Name (assumed stored in BranchId)
                        ExpiryDate = _context.GRNDetails.Where(g => g.ProductId == td.ProductId && g.GRNHeader.GRNNumber == td.BatchNumber).Select(g => g.ExpDate).FirstOrDefault(),
                        ManufacturingDate = _context.GRNDetails.Where(g => g.ProductId == td.ProductId && g.GRNHeader.GRNNumber == td.BatchNumber).Select(g => g.MfgDate).FirstOrDefault(),
                        CurrentStock = item.AvailableStock
                    })
                    .ToListAsync();

                var transferOutHistory = await _context.StockTransferDetails
                    .Where(td => td.CompanyId == companyId && td.ProductId == item.ProductId && td.StockTransferHeader.FromWarehouseId == item.WarehouseId)
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
                        SoldQty = td.Quantity, 
                        AvailableQty = -td.Quantity,
                        WarehouseName = item.WarehouseName,
                        RackName = item.RackName,
                        BranchId = td.BranchId ?? td.StockTransferHeader.FromWarehouse.BranchId, // Fallback to warehouse branch
                        ExpiryDate = _context.GRNDetails.Where(g => g.ProductId == td.ProductId && g.GRNHeader.GRNNumber == td.BatchNumber).Select(g => g.ExpDate).FirstOrDefault(),
                        ManufacturingDate = _context.GRNDetails.Where(g => g.ProductId == td.ProductId && g.GRNHeader.GRNNumber == td.BatchNumber).Select(g => g.MfgDate).FirstOrDefault(),
                        CurrentStock = item.AvailableStock
                    })
                    .ToListAsync();

                var history = grnHistory.Concat(transferInHistory).Concat(transferOutHistory)
                    .OrderBy(h => h.ReceivedDate)
                    .ToList();

                // Apply FIFO distribution of TotalSold and TotalPurchaseReturn
                decimal remainingSold = item.TotalSold;
                decimal remainingReturn = totalPurchaseReturn;

                foreach (var h in history)
                {
                    decimal netRecv = h.ReceivedQty - h.RejectedQty;
                    
                    // 1. Deduct Purchase Returns first (FIFO)
                    if (remainingReturn > 0)
                    {
                        decimal toReturn = Math.Min(remainingReturn, netRecv);
                        h.ReturnedQty = toReturn;
                        netRecv -= toReturn;
                        remainingReturn -= toReturn;
                    }

                    // 2. Deduct Sales (FIFO)
                    if (remainingSold > 0 && netRecv > 0)
                    {
                        decimal toSold = Math.Min(remainingSold, netRecv);
                        h.SoldQty = toSold;
                        netRecv -= toSold;
                        remainingSold -= toSold;
                    }

                    h.AvailableQty = netRecv;
                }
                item.History = history;
            }

            return new StockPagedResponseDto
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<StockPagedResponseDto> GetDisposedStockAsync(string? search, string? sortField, string? sortOrder, int pageIndex, int pageSize, DateTime? startDate, DateTime? endDate, Guid? warehouseId = null, Guid? rackId = null)
        {
            // Disposed stock is essentially current stock with showPurged=true
            // but we filter it to only show items that have actual rejected or expired quantities
            var stockData = await GetCurrentStockAsync(search, sortField, sortOrder, 0, 1000, startDate, endDate, warehouseId, rackId, true);
            
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
            var data = await _context.WarehouseStocks
                .Include(ws => ws.Product)
                .Include(ws => ws.Warehouse)
                .Where(ws => productIds.Contains(ws.ProductId) && ws.CompanyId == companyId)
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


        public async Task<object> GetWarehouseStockAsync(string? search, string? sortField, string? sortOrder, int pageIndex, int pageSize, Guid? productId = null, Guid? warehouseId = null)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;

            var query = _context.WarehouseStocks
                .Include(ws => ws.Product)
                .Include(ws => ws.Warehouse)
                .Where(ws => ws.CompanyId == companyId)
                .AsQueryable();

            if (productId.HasValue && productId != Guid.Empty)
            {
                query = query.Where(ws => ws.ProductId == productId);
            }

            if (warehouseId.HasValue && warehouseId != Guid.Empty)
            {
                query = query.Where(ws => ws.WarehouseId == warehouseId);
            }

            if (!string.IsNullOrEmpty(branchId))
            {
                query = query.Where(ws => ws.BranchId == branchId);
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
                    IsLowStock = ws.Quantity <= ws.MinStock
                })
                .ToListAsync();

            return new { items, totalCount };
        }

        public async Task<List<BatchTransactionDto>> GetBatchTransactionsAsync(Guid productId, Guid warehouseId, Guid rackId, DateTime? mfgDate, DateTime? expDate)
        {
            // Placeholder - Implement if needed for details view
            return new List<BatchTransactionDto>();
        }
    }
}
