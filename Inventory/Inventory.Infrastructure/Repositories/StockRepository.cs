using ClosedXML.Excel;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.GRN.DTOs.Stock;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Linq;

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



        //public async Task<StockPagedResponseDto> GetCurrentStockAsync(
        // string? search,
        // string? sortField,
        // string? sortOrder,
        // int pageIndex,
        // int pageSize,
        // DateTime? startDate = null,
        // DateTime? endDate = null)
        //    {
        //        // 1. Base Query on GRNDetails with Date Filters applied first for performance
        //        var baseQuery = _context.GRNDetails.AsQueryable();

        //        if (startDate.HasValue)
        //        {
        //            baseQuery = baseQuery.Where(x => x.GRNHeader.ReceivedDate >= startDate.Value);
        //        }
        //        if (endDate.HasValue)
        //        {
        //            baseQuery = baseQuery.Where(x => x.GRNHeader.ReceivedDate <= endDate.Value);
        //        }

        //        // 2. Optimized Grouping Logic
        //        var query = baseQuery
        //            .GroupBy(g => new
        //            {
        //                ProductId = g.ProductId,
        //                ProductName = g.Product.Name,
        //                UnitName = g.Product.Unit,
        //                MinStock = g.Product.MinStock
        //            })
        //            .Select(group => new StockSummaryDto
        //            {
        //                ProductId = group.Key.ProductId,
        //                ProductName = group.Key.ProductName,
        //                Unit = group.Key.UnitName,
        //                MinStockLevel = group.Key.MinStock,

        //                TotalReceived = group.Sum(x => x.ReceivedQty),
        //                TotalRejected = group.Sum(x => x.RejectedQty),
        //                AvailableStock = group.Sum(x => x.ReceivedQty) - group.Sum(x => x.RejectedQty),

        //                LastRate = group.OrderByDescending(x => x.Id).Select(x => x.UnitRate).FirstOrDefault(),
        //                LastPurchaseOrderId = group.OrderByDescending(x => x.Id).Select(x => x.GRNHeader.PurchaseOrderId).FirstOrDefault(),
        //                LastSupplierId = group.OrderByDescending(x => x.Id).Select(x => x.GRNHeader.PurchaseOrder.SupplierId).FirstOrDefault(),

        //                History = group.OrderByDescending(x => x.GRNHeader.ReceivedDate)
        //                               .SelectMany(h => _context.GRNDetails
        //                                   .Where(allG => allG.GRNHeaderId == h.GRNHeaderId)
        //                                   .Select(allG => new StockHistoryDto
        //                                   {
        //                                       ReceivedDate = allG.GRNHeader.ReceivedDate,
        //                                       PONumber = allG.GRNHeader.PurchaseOrder.PoNumber,
        //                                       SupplierName = allG.GRNHeader.PurchaseOrder.SupplierName,
        //                                       ProductName = allG.Product.Name,
        //                                       ReceivedQty = allG.ReceivedQty,
        //                                       RejectedQty = allG.RejectedQty
        //                                   })).ToList()
        //            });

        //        // 3. Search Logic
        //        if (!string.IsNullOrEmpty(search))
        //        {
        //            query = query.Where(x => x.ProductName.Contains(search));
        //        }

        //        // 4. FIXED Dynamic Sorting: Added 'totalreceived' case
        //        bool isDesc = sortOrder?.ToLower() == "desc";
        //        query = sortField?.ToLower() switch
        //        {
        //            "productname" => isDesc ? query.OrderByDescending(x => x.ProductName) : query.OrderBy(x => x.ProductName),
        //            "totalreceived" => isDesc ? query.OrderByDescending(x => x.TotalReceived) : query.OrderBy(x => x.TotalReceived), // Added Fix
        //            "availablestock" => isDesc ? query.OrderByDescending(x => x.AvailableStock) : query.OrderBy(x => x.AvailableStock),
        //            "totalrejected" => isDesc ? query.OrderByDescending(x => x.TotalRejected) : query.OrderBy(x => x.TotalRejected),
        //            "unitrate" => isDesc ? query.OrderByDescending(x => x.LastRate) : query.OrderBy(x => x.LastRate),
        //            _ => query.OrderBy(x => x.ProductName)
        //        };

        //        // 5. Final Execution with Pagination
        //        var totalCount = await query.CountAsync();
        //        var items = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();

        //        return new StockPagedResponseDto
        //        {
        //            Items = items,
        //            TotalCount = totalCount
        //        };
        //    }

        //    public async Task<StockPagedResponseDto> GetCurrentStockAsync(
        //string? search,
        //string? sortField,
        //string? sortOrder,
        //int pageIndex,
        //int pageSize,
        //DateTime? startDate = null,
        //DateTime? endDate = null)
        //    {
        //        // STEP 1: Sirf Base GRN data grouping karein (Sales aur History ke bina)
        //        // Ye query ekdum light hai aur kabhi timeout nahi degi
        //        var baseQuery = _context.GRNDetails.AsQueryable();

        //        if (startDate.HasValue)
        //            baseQuery = baseQuery.Where(x => x.GRNHeader.ReceivedDate >= startDate.Value);
        //        if (endDate.HasValue)
        //            baseQuery = baseQuery.Where(x => x.GRNHeader.ReceivedDate <= endDate.Value);

        //        var groupedQuery = baseQuery
        //            .GroupBy(g => new
        //            {
        //                g.ProductId,
        //                ProductName = g.Product.Name,
        //                UnitName = g.Product.Unit,
        //                MinStock = g.Product.MinStock
        //            })
        //            .Select(group => new StockSummaryDto
        //            {
        //                ProductId = group.Key.ProductId,
        //                ProductName = group.Key.ProductName,
        //                Unit = group.Key.UnitName,
        //                MinStockLevel = group.Key.MinStock,
        //                TotalReceived = group.Sum(x => x.ReceivedQty),
        //                TotalRejected = group.Sum(x => x.RejectedQty),
        //                // Initial available stock (Sales minus karne se pehle)
        //                AvailableStock = group.Sum(x => x.ReceivedQty) - group.Sum(x => x.RejectedQty),
        //                LastRate = group.OrderByDescending(x => x.Id).Select(x => x.UnitRate).FirstOrDefault(),
        //                LastPurchaseOrderId = group.OrderByDescending(x => x.Id).Select(x => x.GRNHeader.PurchaseOrderId).FirstOrDefault()
        //            });

        //        // Search Logic
        //        if (!string.IsNullOrEmpty(search))
        //        {
        //            groupedQuery = groupedQuery.Where(x => x.ProductName.Contains(search));
        //        }

        //        // Sorting Logic
        //        bool isDesc = sortOrder?.ToLower() == "desc";
        //        groupedQuery = sortField?.ToLower() switch
        //        {
        //            "productname" => isDesc ? groupedQuery.OrderByDescending(x => x.ProductName) : groupedQuery.OrderBy(x => x.ProductName),
        //            "totalreceived" => isDesc ? groupedQuery.OrderByDescending(x => x.TotalReceived) : groupedQuery.OrderBy(x => x.TotalReceived),
        //            "availablestock" => isDesc ? groupedQuery.OrderByDescending(x => x.AvailableStock) : groupedQuery.OrderBy(x => x.AvailableStock),
        //            _ => groupedQuery.OrderBy(x => x.ProductName)
        //        };

        //        // STEP 2: Pagination execute karke sirf limited items (e.g., 10 items) layein
        //        var totalCount = await groupedQuery.CountAsync();
        //        var items = await groupedQuery.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();

        //        // STEP 3: Ab sirf in 10 items ke liye Sales aur History fetch karein
        //        // Ye loop sirf 10 baar chalega, isliye performance par koi asar nahi padega
        //        foreach (var item in items)
        //        {
        //            // 1. Calculate Total Sold for this product
        //            item.TotalSold = await _context.SaleOrderItems
        //                .Where(si => si.ProductId == item.ProductId && si.SaleOrder.Status == "Confirmed")
        //                .SumAsync(si => (decimal?)si.Qty) ?? 0;

        //            // 2. Final Stock Update
        //            item.AvailableStock = (item.TotalReceived - item.TotalRejected) - item.TotalSold;

        //            // 3. History fetch (Sirf is product ki specific history)
        //            item.History = await _context.GRNDetails
        //                .Where(g => g.ProductId == item.ProductId)
        //                .OrderByDescending(g => g.GRNHeader.ReceivedDate)
        //                .Take(15) // Sirf top 15 records dikhayein speed ke liye
        //                .Select(allG => new StockHistoryDto
        //                {
        //                    ReceivedDate = allG.GRNHeader.ReceivedDate,
        //                    PONumber = allG.GRNHeader.PurchaseOrder.PoNumber,
        //                    SupplierName = allG.GRNHeader.PurchaseOrder.SupplierName,
        //                    ProductName = allG.Product.Name,
        //                    ReceivedQty = allG.ReceivedQty,
        //                    RejectedQty = allG.RejectedQty
        //                }).ToListAsync();
        //        }

        //        return new StockPagedResponseDto
        //        {
        //            Items = items,
        //            TotalCount = totalCount
        //        };
        //    }

        // public async Task<StockPagedResponseDto> GetCurrentStockAsync(
        //string? search,
        //string? sortField,
        //string? sortOrder,
        //int pageIndex,
        //int pageSize,
        //DateTime? startDate = null,
        //DateTime? endDate = null)
        // {
        //     // STEP 1: Base Query - GRNDetails se start karenge traceability ke liye
        //     var baseQuery = _context.GRNDetails.AsNoTracking().AsQueryable();

        //     if (startDate.HasValue)
        //         baseQuery = baseQuery.Where(x => x.GRNHeader.ReceivedDate >= startDate.Value);
        //     if (endDate.HasValue)
        //         baseQuery = baseQuery.Where(x => x.GRNHeader.ReceivedDate <= endDate.Value);

        //     // STEP 2: Grouping Logic - Product wise aggregate [cite: 2026-02-04]
        //     var groupedQuery = baseQuery
        //         .GroupBy(g => new
        //         {
        //             g.ProductId,
        //             ProductName = g.Product.Name,
        //             UnitName = g.Product.Unit,
        //             MinStock = g.Product.MinStock,
        //             // DIRECT LINK: Products table ka CurrentStock column
        //             ActualCurrentStock = g.Product.CurrentStock
        //         })
        //         .Select(group => new StockSummaryDto
        //         {
        //             ProductId = group.Key.ProductId,
        //             ProductName = group.Key.ProductName,
        //             Unit = group.Key.UnitName,
        //             MinStockLevel = group.Key.MinStock,

        //             // TotalReceived: GRN se total kitna aaya
        //             TotalReceived = group.Sum(x => x.ReceivedQty),
        //             TotalRejected = group.Sum(x => x.RejectedQty),

        //             // FIX: Calculation ki jagah direct DB Column bind karein
        //             AvailableStock = group.Key.ActualCurrentStock,

        //             LastRate = group.OrderByDescending(x => x.Id).Select(x => x.UnitRate).FirstOrDefault(),
        //             LastPurchaseOrderId = group.OrderByDescending(x => x.Id).Select(x => x.GRNHeader.PurchaseOrderId).FirstOrDefault()
        //         });

        //     // STEP 3: Search & Sorting
        //     if (!string.IsNullOrEmpty(search))
        //         groupedQuery = groupedQuery.Where(x => x.ProductName.Contains(search));

        //     bool isDesc = sortOrder?.ToLower() == "desc";
        //     groupedQuery = sortField?.ToLower() switch
        //     {
        //         "productname" => isDesc ? groupedQuery.OrderByDescending(x => x.ProductName) : groupedQuery.OrderBy(x => x.ProductName),
        //         "totalreceived" => isDesc ? groupedQuery.OrderByDescending(x => x.TotalReceived) : groupedQuery.OrderBy(x => x.TotalReceived),
        //         "availablestock" => isDesc ? groupedQuery.OrderByDescending(x => x.AvailableStock) : groupedQuery.OrderBy(x => x.AvailableStock),
        //         _ => groupedQuery.OrderBy(x => x.ProductName)
        //     };

        //     var totalCount = await groupedQuery.CountAsync();
        //     var items = await groupedQuery.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();

        //     // STEP 4: Real-Time Stats (Without overriding AvailableStock)
        //     foreach (var item in items)
        //     {
        //         // 1. Confirmed Sales fetch karein sirf information ke liye
        //         item.TotalSold = await _context.SaleOrderItems
        //             .Where(si => si.ProductId == item.ProductId &&
        //                         (si.SaleOrder.Status == "Confirmed" || si.SaleOrder.Status == "Completed"))
        //             .SumAsync(si => (decimal?)si.Qty) ?? 0;

        //         // 2. Audit Trail Logic (History list)
        //         item.History = await _context.GRNDetails
        //             .Where(g => g.ProductId == item.ProductId)
        //             .OrderByDescending(g => g.GRNHeader.ReceivedDate)
        //             .Take(10)
        //             .Select(allG => new StockHistoryDto
        //             {
        //                 ReceivedDate = allG.GRNHeader.ReceivedDate,
        //                 PONumber = allG.GRNHeader.PurchaseOrder.PoNumber,
        //                 SupplierName = allG.GRNHeader.PurchaseOrder.SupplierName,
        //                 ProductName = allG.Product.Name,
        //                 ReceivedQty = allG.ReceivedQty,
        //                 RejectedQty = allG.RejectedQty
        //             }).ToListAsync();

        //         // NOTE: Humne yahan 'item.AvailableStock =' waali manual calculation hata di hai 
        //         // taaki wo Products table ke data ko overwrite na kare. [cite: 2026-02-06]
        //     }

        //     return new StockPagedResponseDto
        //     {
        //         Items = items,
        //         TotalCount = totalCount
        //     };
        // }


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
        bool showPurged = false)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            // STEP 1: Base Query - GRNDetails se start karenge traceability ke liye
            var baseQuery = _context.GRNDetails.AsNoTracking().Where(x => x.CompanyId == companyId).AsQueryable();

            if (startDate.HasValue)
                baseQuery = baseQuery.Where(x => x.GRNHeader.ReceivedDate.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                baseQuery = baseQuery.Where(x => x.GRNHeader.ReceivedDate.Date <= endDate.Value.Date);

            if (warehouseId.HasValue && warehouseId.Value != Guid.Empty)
                baseQuery = baseQuery.Where(x => x.WarehouseId == warehouseId.Value);
            if (rackId.HasValue && rackId.Value != Guid.Empty)
                baseQuery = baseQuery.Where(x => x.RackId == rackId.Value);

            // STEP 2: Grouping Logic - Product wise aggregate [cite: 2026-02-04]
            var groupedQuery = baseQuery
                .GroupBy(g => new
                {
                    g.ProductId,
                    ProductName = g.Product.Name,
                    UnitName = g.Product.Unit,
                    MinStock = g.Product.MinStock,
                    ActualCurrentStock = g.Product.CurrentStock,
                    g.WarehouseId,
                    WarehouseName = g.Warehouse != null ? g.Warehouse.Name : "N/A",
                    g.RackId,
                    RackName = g.Rack != null ? g.Rack.Name : "N/A",
                    Sku = g.Product.Sku,
                    GstPercent = g.Product.DefaultGst ?? 0,
                    IsExpiryRequired = g.Product.IsExpiryRequired
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

                    // TotalReceived: Gross inward quantity
                    TotalReceived = group.Sum(x => x.ReceivedQty),
                    TotalRejected = group.Sum(x => (x.Rack != null && (
                        x.Rack.Name.ToLower().Contains("e1") || 
                        (x.Rack.Description != null && (
                            x.Rack.Description.ToLower().Contains("expired") || 
                            x.Rack.Description.ToLower().Contains("damaged") || 
                            x.Rack.Description.ToLower().Contains("rejected") ||
                            x.Rack.Description.ToLower().Contains("purged")
                        ))
                    )) ? 0 : x.RejectedQty),
                    TotalExpired = group.Sum(x => (x.Rack != null && (
                        x.Rack.Name.ToLower().Contains("e1") || 
                        (x.Rack.Description != null && (
                            x.Rack.Description.ToLower().Contains("expired") || 
                            x.Rack.Description.ToLower().Contains("damaged") || 
                            x.Rack.Description.ToLower().Contains("rejected") ||
                            x.Rack.Description.ToLower().Contains("purged")
                        ))
                    )) ? x.RejectedQty : 0),

                    // AvailableStock: Inward balance for this specific location
                    AvailableStock = group.Sum(x => x.ReceivedQty - x.RejectedQty),

                    LastRate = group.OrderByDescending(x => x.Id).Select(x => x.UnitRate).FirstOrDefault(),
                    LastPurchaseOrderId = group.OrderByDescending(x => x.Id).Select(x => x.GRNHeader.PurchaseOrderId).FirstOrDefault()
                    // ManufacturingDate & ExpiryDate: computed in STEP 4 loop (earliest expiry batch logic)
                });

            // STEP 2B: Filter out entirely empty locations
            // We only show empty rows if:
            // 1. It's an Expired Rack (to see PURGED history) - only if showPurged is ON
            // 2. It's a regular rack but has some inwarded qty (to see SOLD OUT history)
            if (!showPurged)
            {
                // Normal view: Show only items with physical stock
                groupedQuery = groupedQuery.Where(x => x.TotalReceived > 0 || x.TotalRejected > 0);
            }
            else
            {
                // Review Purged view: Show items with stock OR items in Expired Racks (for history)
                groupedQuery = groupedQuery.Where(x => 
                    x.TotalReceived > 0 || 
                    x.TotalRejected > 0 || 
                    (x.RackName.Contains("E1") || (x.RackName != null && x.RackName.Contains("Expired")))
                );
            }

            // STEP 3: Search & Sorting
            if (!string.IsNullOrEmpty(search))
                groupedQuery = groupedQuery.Where(x => x.ProductName.Contains(search));

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

            // STEP 4: Real-Time Stats (Net Sale Calculation) [cite: 2026-02-06]
            foreach (var item in items)
            {
                // 1. Gross Sold fetch karein (Confirmed/Completed)
                var grossSold = await _context.SaleOrderItems
                    .Where(si => si.CompanyId == companyId &&
                                si.ProductId == item.ProductId && 
                                si.WarehouseId == item.WarehouseId && 
                                si.RackId == item.RackId &&
                                (si.SaleOrder.Status == "Confirmed" || si.SaleOrder.Status == "Completed"))
                    .SumAsync(si => (decimal?)si.Qty) ?? 0;

                // 2. Sale Return fetch karein (Confirmed and Inwarded returns)
                var totalSaleReturn = await _context.SaleReturnItems
                    .Where(sri => sri.ProductId == item.ProductId && 
                                 sri.WarehouseId == item.WarehouseId && 
                                 sri.RackId == item.RackId &&
                                 sri.CompanyId == companyId &&
                                 (sri.SaleReturnHeader.Status == "Confirmed" || sri.SaleReturnHeader.Status == "INWARDED"))
                    .SumAsync(sri => (decimal?)sri.ReturnQty) ?? 0;

                // 4. Update Net Stats
                item.TotalSold = grossSold - totalSaleReturn;
                
                // 🎯 5. Final Stock Update: Physical Stock Calculation
                // Rule: Show what's in the rack until it is completely deleted (purged)
                // Inward - Rejected - Sold
                item.AvailableStock = item.TotalReceived - item.TotalRejected - item.TotalSold;

                // 6. BUSINESS RULE: Main row mein sabse pehle expire hone wali batch dikhao
                //    In normal racks, check usable stock (Received - Rejected > 0). 
                //    In Expired racks, check gross received so we still see the date.
                var earliestBatch = await _context.GRNDetails
                    .Where(g => g.ProductId == item.ProductId && g.WarehouseId == item.WarehouseId && g.RackId == item.RackId && g.ExpDate != null)
                    .OrderBy(g => g.ExpDate)
                    .Select(g => new { g.MfgDate, g.ExpDate })
                    .FirstOrDefaultAsync();

                if (earliestBatch != null)
                {
                    item.ManufacturingDate = earliestBatch.MfgDate;
                    item.ExpiryDate = earliestBatch.ExpDate;
                }
                else
                {
                    var latestBatch = await _context.GRNDetails
                        .Where(g => g.ProductId == item.ProductId && g.WarehouseId == item.WarehouseId && g.RackId == item.RackId)
                        .OrderByDescending(g => g.Id)
                        .Select(g => new { g.MfgDate, g.ExpDate })
                        .FirstOrDefaultAsync();
                    item.ManufacturingDate = latestBatch?.MfgDate;
                    item.ExpiryDate = latestBatch?.ExpDate;
                }

                // 7. Audit Trail History & Batch-wise Stock Calculation
                var allBatches = await _context.GRNDetails
                    .Where(g => g.ProductId == item.ProductId && g.WarehouseId == item.WarehouseId && g.RackId == item.RackId)
                    .OrderBy(g => g.ExpDate ?? DateTime.MaxValue)
                    .ThenBy(g => g.GRNHeader.ReceivedDate)
                    .Select(allG => new StockHistoryDto
                    {
                        ProductId = allG.ProductId,
                        WarehouseId = allG.WarehouseId,
                        RackId = allG.RackId,
                        ReceivedDate = allG.GRNHeader.ReceivedDate.AddHours(5).AddMinutes(30),
                        PONumber = allG.GRNHeader.PurchaseOrder.PoNumber,
                        GRNNumber = allG.GRNHeader.GRNNumber,
                        TransactionType = allG.GRNHeader.IsQuick ? "QuickGRN" : "GRN",
                        SupplierName = allG.GRNHeader.PurchaseOrder.SupplierName,
                        ProductName = allG.Product.Name,
                        ReceivedQty = allG.ReceivedQty,
                        ExpiredQty = (allG.Rack != null && (
                            allG.Rack.Name.ToLower().Contains("e1") || 
                            (allG.Rack.Description != null && (
                                allG.Rack.Description.ToLower().Contains("expired") || 
                                allG.Rack.Description.ToLower().Contains("damaged") || 
                                allG.Rack.Description.ToLower().Contains("rejected") ||
                                allG.Rack.Description.ToLower().Contains("purged")
                            ))
                        )) ? allG.RejectedQty : 0,
                        RejectedQty = (allG.Rack != null && (
                            allG.Rack.Name.ToLower().Contains("e1") || 
                            (allG.Rack.Description != null && (
                                allG.Rack.Description.ToLower().Contains("expired") || 
                                allG.Rack.Description.ToLower().Contains("damaged") || 
                                allG.Rack.Description.ToLower().Contains("rejected") ||
                                allG.Rack.Description.ToLower().Contains("purged")
                            ))
                        )) ? 0 : allG.RejectedQty,
                        WarehouseName = allG.Warehouse != null ? allG.Warehouse.Name : "N/A",
                        RackName = allG.Rack != null ? allG.Rack.Name : "N/A",
                        ManufacturingDate = allG.MfgDate,
                        ExpiryDate = allG.ExpDate,
                        IsExpiryRequired = allG.Product.IsExpiryRequired,
                        AvailableQty = allG.ReceivedQty - allG.RejectedQty // Net Initial
                    }).ToListAsync();

                // 🎯 7B. Fetch Purge History for these batches (since we reduce ReceivedQty on purge, we must check transactions)
                foreach (var h in allBatches)
                {
                    // Dynamic identification repeat for filtering transactions if needed, but we check by ID or Batch
                    var purgeQty = await _context.InventoryTransactions
                        .Where(it => it.ProductId == h.ProductId && 
                                     it.WarehouseId == h.WarehouseId && 
                                     it.RackId == h.RackId && 
                                     (it.TransactionType == "StockPurge-OUT" || it.TransactionType == "StockAdjustment-OUT") &&
                                     it.ExpDate.HasValue && h.ExpiryDate.HasValue && it.ExpDate.Value.Date == h.ExpiryDate.Value.Date)
                        .SumAsync(it => (decimal?)it.Quantity) ?? 0;

                    if (purgeQty > 0)
                    {
                        h.IsAlreadyPurged = true;
                        // 🛠️ FIX: Restore visual representation for purged items
                        // We show the quantity that WAS expired/received before the purge
                        if (h.ReceivedQty == 0) 
                        {
                            h.ReceivedQty = purgeQty; 
                            h.ExpiredQty = purgeQty;
                            // h.AvailableQty remains 0 (Received - Rejected = 0 because Rejected was also reduced)
                        }
                        else 
                        {
                            // If it was only partially purged
                            h.ExpiredQty += purgeQty;
                            h.ReceivedQty += purgeQty;
                        }
                    }
                }

                // 🎯 Calculate Batch-wise "Sold" strictly (Net of Sales and Returns)
                var grossSales = await _context.SaleOrderItems
                    .Where(si => si.CompanyId == companyId && si.ProductId == item.ProductId && si.WarehouseId == item.WarehouseId && si.RackId == item.RackId && (si.SaleOrder.Status == "Confirmed" || si.SaleOrder.Status == "Completed"))
                    .Select(si => new { si.Qty, si.MfgDate, si.ExpDate })
                    .ToListAsync();

                var batchReturns = await _context.SaleReturnItems
                    .Where(sri => sri.CompanyId == companyId && sri.ProductId == item.ProductId && sri.WarehouseId == item.WarehouseId && sri.RackId == item.RackId && (sri.SaleReturnHeader.Status == "Confirmed" || sri.SaleReturnHeader.Status == "INWARDED"))
                    .Select(sri => new { Qty = sri.ReturnQty, sri.MfgDate, sri.ExpDate })
                    .ToListAsync();

                // Group both by Batch (Mfg/Exp) to get Net Sold per batch
                var specificSales = grossSales.Where(s => s.MfgDate != null || s.ExpDate != null)
                    .GroupBy(s => new { Mfg = s.MfgDate?.Date, Exp = s.ExpDate?.Date })
                    .Select(g => new { MfgDate = g.Key.Mfg, ExpDate = g.Key.Exp, Qty = g.Sum(x => x.Qty) })
                    .ToList();

                var specificReturns = batchReturns.Where(r => r.MfgDate != null || r.ExpDate != null)
                    .GroupBy(r => new { Mfg = r.MfgDate?.Date, Exp = r.ExpDate?.Date })
                    .Select(g => new { MfgDate = g.Key.Mfg, ExpDate = g.Key.Exp, Qty = g.Sum(x => x.Qty) })
                    .ToList();

                var genericSalesSum = grossSales.Where(s => s.MfgDate == null && s.ExpDate == null).Sum(s => s.Qty);
                var genericReturnsSum = batchReturns.Where(r => r.MfgDate == null && r.ExpDate == null).Sum(r => r.Qty);

                // 1. Deduct Net Specific Sales from their matching batches
                foreach (var sSale in specificSales)
                {
                    var sReturnQty = specificReturns.FirstOrDefault(r => 
                        (r.MfgDate == null && sSale.MfgDate == null || r.MfgDate != null && sSale.MfgDate != null && r.MfgDate.Value.Date == sSale.MfgDate.Value.Date) && 
                        (r.ExpDate == null && sSale.ExpDate == null || r.ExpDate != null && sSale.ExpDate != null && r.ExpDate.Value.Date == sSale.ExpDate.Value.Date))?.Qty ?? 0;
                    var netBatchSold = sSale.Qty - sReturnQty;

                    if (netBatchSold > 0)
                    {
                        var matchingBatch = allBatches.FirstOrDefault(b => 
                            (b.ManufacturingDate == null && sSale.MfgDate == null || b.ManufacturingDate != null && sSale.MfgDate != null && b.ManufacturingDate.Value.Date == sSale.MfgDate.Value.Date) && 
                            (b.ExpiryDate == null && sSale.ExpDate == null || b.ExpiryDate != null && sSale.ExpDate != null && b.ExpiryDate.Value.Date == sSale.ExpDate.Value.Date) && 
                            b.AvailableQty > 0);
                        if (matchingBatch != null)
                        {
                            matchingBatch.AvailableQty -= netBatchSold;
                        }
                        else
                        {
                            genericSalesSum += netBatchSold; // Spillover if exact batch not found
                        }
                    }
                }

                // 2. Deduct Net Generic Sales using FIFO (Oldest first)
                var netGenericSold = genericSalesSum - genericReturnsSum;
                if (netGenericSold > 0)
                {
                    var oldestFirstBatches = allBatches.OrderBy(b => b.ReceivedDate).ToList();
                    foreach (var batch in oldestFirstBatches)
                    {
                        if (netGenericSold <= 0) break;
                        if (batch.AvailableQty <= 0) continue;

                        if (netGenericSold >= batch.AvailableQty)
                        {
                            netGenericSold -= batch.AvailableQty;
                            batch.AvailableQty = 0;
                        }
                        else
                        {
                            batch.AvailableQty -= netGenericSold;
                            netGenericSold = 0;
                        }
                    }
                }

                item.History = allBatches.Take(15).ToList(); // Return top 15 batches with real AvailableQty
                
                // 🎯 If the location has no physical stock left but was once purged (Expired Rack case)
                if (item.AvailableStock == 0 && item.History.Any(h => h.IsAlreadyPurged))
                {
                    item.IsAlreadyPurged = true;
                }
            }

            return new StockPagedResponseDto
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<StockPagedResponseDto> GetDisposedStockAsync(
            string? search,
            string? sortField,
            string? sortOrder,
            int pageIndex,
            int pageSize,
            DateTime? startDate = null,
            DateTime? endDate = null,
            Guid? warehouseId = null,
            Guid? rackId = null)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            // Similar to current stock but focused on Rejected Items (Disposed)
            var baseQuery = _context.GRNDetails.AsNoTracking().Where(x => x.CompanyId == companyId).AsQueryable();

            if (startDate.HasValue)
                baseQuery = baseQuery.Where(x => x.GRNHeader.ReceivedDate >= startDate.Value);
            if (endDate.HasValue)
                baseQuery = baseQuery.Where(x => x.GRNHeader.ReceivedDate <= endDate.Value);

             // 🎯 KEY FILTER: Only items having some non-zero rejected qty at that location
            baseQuery = baseQuery.Where(x => x.RejectedQty > 0);

            if (warehouseId.HasValue && warehouseId.Value != Guid.Empty)
                baseQuery = baseQuery.Where(x => x.WarehouseId == warehouseId.Value);
            if (rackId.HasValue && rackId.Value != Guid.Empty)
                baseQuery = baseQuery.Where(x => x.RackId == rackId.Value);

            var groupedQuery = baseQuery
                .GroupBy(g => new
                {
                    g.ProductId,
                    ProductName = g.Product.Name,
                    UnitName = g.Product.Unit,
                    g.WarehouseId,
                    WarehouseName = g.Warehouse != null ? g.Warehouse.Name : "N/A",
                    g.RackId,
                    RackName = g.Rack != null ? g.Rack.Name : "N/A"
                })
                .Select(group => new StockSummaryDto
                {
                    ProductId = group.Key.ProductId,
                    ProductName = group.Key.ProductName,
                    Unit = group.Key.UnitName,
                    WarehouseId = group.Key.WarehouseId,
                    WarehouseName = group.Key.WarehouseName,
                    RackId = group.Key.RackId,
                    RackName = group.Key.RackName,

                    // Summary for Disposed
                    TotalReceived = group.Sum(x => x.ReceivedQty),
                    TotalRejected = group.Sum(x => x.RejectedQty),
                    AvailableStock = group.Sum(x => x.RejectedQty), // HACK: reusing availablestock as "Disposed Stock" for UI display

                    LastRate = group.OrderByDescending(x => x.Id).Select(x => x.UnitRate).FirstOrDefault()
                });

            if (!string.IsNullOrEmpty(search))
                groupedQuery = groupedQuery.Where(x => x.ProductName.Contains(search));

            var totalCount = await groupedQuery.CountAsync();
            var items = await groupedQuery.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();

            foreach (var item in items)
            {
                // Detailed history for specific product + location (Rejected Batches only)
                item.History = await _context.GRNDetails
                    .Where(g => g.ProductId == item.ProductId && g.RejectedQty > 0 && g.WarehouseId == item.WarehouseId && g.RackId == item.RackId)
                    .OrderByDescending(g => g.GRNHeader.ReceivedDate)
                    .Select(allG => new StockHistoryDto
                    {
                        ProductId = allG.ProductId,
                        WarehouseId = allG.WarehouseId,
                        RackId = allG.RackId,
                        ReceivedDate = allG.GRNHeader.ReceivedDate.AddHours(5).AddMinutes(30),
                        PONumber = allG.GRNHeader.PurchaseOrder.PoNumber,
                        GRNNumber = allG.GRNHeader.GRNNumber,
                        SupplierName = allG.GRNHeader.PurchaseOrder.SupplierName,
                        ProductName = allG.Product.Name,
                        ReceivedQty = allG.ReceivedQty,
                        ExpiredQty = (allG.Rack != null && (allG.Rack.Name.Contains("E1") || (allG.Rack.Description != null && allG.Rack.Description.Contains("Expired")))) ? allG.RejectedQty : 0,
                        RejectedQty = (allG.Rack != null && (allG.Rack.Name.Contains("E1") || (allG.Rack.Description != null && allG.Rack.Description.Contains("Expired")))) ? 0 : allG.RejectedQty,
                        WarehouseName = allG.Warehouse != null ? allG.Warehouse.Name : "N/A",
                        RackName = allG.Rack != null ? allG.Rack.Name : "N/A",
                        ManufacturingDate = allG.MfgDate,
                        ExpiryDate = allG.ExpDate,
                        IsExpiryRequired = allG.Product.IsExpiryRequired
                    }).ToListAsync();
            }

            return new StockPagedResponseDto { Items = items, TotalCount = totalCount };
        }

        public async Task<byte[]> GenerateStockExcel(List<Guid> productIds)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var stockData = await _context.GRNDetails
                .Where(x => productIds.Contains(x.ProductId) && x.CompanyId == companyId)
                .Include(x => x.Product)
                .GroupBy(x => new {
                    x.ProductId,
                    ProductName = x.Product.Name,
                    MinLevel = x.Product.MinStock,
                    ActualStock = x.Product.CurrentStock,
                    WarehouseName = x.Warehouse != null ? x.Warehouse.Name : "N/A",
                    RackName = x.Rack != null ? x.Rack.Name : "N/A"
                })
                .Select(g => new {
                    ProductName = g.Key.ProductName,
                    WarehouseName = g.Key.WarehouseName,
                    RackName = g.Key.RackName,
                    TotalReceived = g.Sum(x => x.ReceivedQty),
                    TotalRejected = g.Sum(x => x.RejectedQty),
                    AvailableStock = g.Key.ActualStock,
                    LastRate = g.OrderByDescending(x => x.Id).Select(x => x.UnitRate).FirstOrDefault(),
                    MinStockLevel = g.Key.MinLevel
                })
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Current Stock");

                // 1. Header Styling
                string[] headers = { "Product Name", "Warehouse", "Rack", "Total Received", "Rejected", "Current Stock", "Value (Avg)", "Total Value" };
                var headerRow = worksheet.Row(1);
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = headerRow.Cell(i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    // Background Fix: SetBackgroundColor aur Pattern Solid use karein
                    cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#3f51b5"));
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                int row = 2;
                foreach (var item in stockData)
                {
                    worksheet.Cell(row, 1).Value = item.ProductName;
                    worksheet.Cell(row, 2).Value = item.WarehouseName;
                    worksheet.Cell(row, 3).Value = item.RackName;
                    worksheet.Cell(row, 4).Value = item.TotalReceived;
                    worksheet.Cell(row, 5).Value = item.TotalRejected;

                    var stockCell = worksheet.Cell(row, 6);
                    stockCell.Value = item.AvailableStock;

                    // 2. RED COLOR LOGIC: Agar stock MinLevel se kam hai
                    if (item.AvailableStock <= item.MinStockLevel)
                    {
                        stockCell.Style.Font.SetFontColor(XLColor.Red);
                        stockCell.Style.Font.Bold = true;
                    }

                    // Rate Column
                    var rateCell = worksheet.Cell(row, 7);
                    rateCell.Value = item.LastRate;
                    rateCell.Style.NumberFormat.Format = "₹ #,##0.00";

                    // Total Value Calculation
                    var totalValCell = worksheet.Cell(row, 8);
                    totalValCell.FormulaA1 = $"=F{row}*G{row}";
                    totalValCell.Style.NumberFormat.Format = "₹ #,##0.00";

                    // 3. ZEBRA STRIPES: Har alternate row par halka grey color
                    if (row % 2 != 0)
                    {
                        // Range select karke poori row ka color set karein
                        worksheet.Range(row, 1, row, 8).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F9FAFB"));
                    }
                    row++;
                }

                // 4. Grand Total Styling
                int lastDataRow = row - 1;
                worksheet.Cell(row, 7).Value = "Total Inventory Value:";
                worksheet.Cell(row, 7).Style.Font.Bold = true;

                var grandTotalCell = worksheet.Cell(row, 8);
                grandTotalCell.FormulaA1 = $"=SUM(H2:H{lastDataRow})";
                grandTotalCell.Style.Font.Bold = true;
                grandTotalCell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9"));
                grandTotalCell.Style.NumberFormat.Format = "₹ #,##0.00";

                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }



        public async Task<List<BatchTransactionDto>> GetBatchTransactionsAsync(
            Guid productId,
            Guid warehouseId,
            Guid rackId,
            DateTime? mfgDate,
            DateTime? expDate)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var query = _context.InventoryTransactions.AsNoTracking()
                .Where(t => t.CompanyId == companyId &&
                            t.ProductId == productId &&
                            t.WarehouseId == warehouseId &&
                            t.RackId == rackId);

            // Batch matching logic (Null safe - comparing Date part only for robustness)
            if (mfgDate.HasValue)
            {
                var targetMfg = mfgDate.Value.Date;
                query = query.Where(t => t.MfgDate != null && t.MfgDate.Value.Date == targetMfg);
            }
            else
                query = query.Where(t => t.MfgDate == null);

            if (expDate.HasValue)
            {
                var targetExp = expDate.Value.Date;
                query = query.Where(t => t.ExpDate != null && t.ExpDate.Value.Date == targetExp);
            }
            else
                query = query.Where(t => t.ExpDate == null);

            var list = await query
                .OrderByDescending(t => t.CreatedOn)
                .Select(t => new BatchTransactionDto
                {
                    TransactionDate = t.CreatedOn.HasValue ? t.CreatedOn.Value.AddHours(5).AddMinutes(30) : DateTime.UtcNow, // IST
                    TransactionType = t.TransactionType,
                    ReferenceId = t.ReferenceId,
                    Quantity = t.Quantity,
                    Category = (new[] { "purchase", "quickgrn", "grn", "salereturn", "quicksalereturn", "expirymove-in" }
                                .Contains(t.TransactionType.ToLower().Trim())) ? "IN" : "OUT"
                })
                .ToListAsync();

            return list;
        }

    }
}
