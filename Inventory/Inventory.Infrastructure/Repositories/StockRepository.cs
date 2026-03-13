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
        private readonly InventoryDbContext _context;

        public StockRepository(InventoryDbContext context)
        {
            _context = context;
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
    Guid? rackId = null)
        {
            // STEP 1: Base Query - GRNDetails se start karenge traceability ke liye
            var baseQuery = _context.GRNDetails.AsNoTracking().AsQueryable();

            if (startDate.HasValue)
                baseQuery = baseQuery.Where(x => x.GRNHeader.ReceivedDate >= startDate.Value);
            if (endDate.HasValue)
                baseQuery = baseQuery.Where(x => x.GRNHeader.ReceivedDate <= endDate.Value);

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
                    RackName = g.Rack != null ? g.Rack.Name : "N/A"
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

                    // TotalReceived: Net usable inward (Received - Rejected)
                    TotalReceived = group.Sum(x => x.ReceivedQty - x.RejectedQty),
                    TotalRejected = group.Sum(x => x.RejectedQty),

                    // AvailableStock: Inward balance for this specific location
                    AvailableStock = group.Sum(x => x.ReceivedQty - x.RejectedQty),

                    LastRate = group.OrderByDescending(x => x.Id).Select(x => x.UnitRate).FirstOrDefault(),
                    LastPurchaseOrderId = group.OrderByDescending(x => x.Id).Select(x => x.GRNHeader.PurchaseOrderId).FirstOrDefault()
                    // ManufacturingDate & ExpiryDate: computed in STEP 4 loop (earliest expiry batch logic)
                });

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
                    .Where(si => si.ProductId == item.ProductId &&
                                (si.SaleOrder.Status == "Confirmed" || si.SaleOrder.Status == "Completed"))
                    .SumAsync(si => (decimal?)si.Qty) ?? 0;

                // 2. Sale Return fetch karein (Confirmed and Inwarded returns)
                var totalSaleReturn = await _context.SaleReturnItems
                    .Where(sri => sri.ProductId == item.ProductId && 
                                 (sri.SaleReturnHeader.Status == "Confirmed" || sri.SaleReturnHeader.Status == "INWARDED"))
                    .SumAsync(sri => (decimal?)sri.ReturnQty) ?? 0;

                // 4. Update Net Stats
                item.TotalSold = grossSold - totalSaleReturn;
                
                // 🎯 5. Final Stock Update: Net Received - Net Sold
                item.AvailableStock = item.TotalReceived - item.TotalSold;

                // 6. BUSINESS RULE: Main row mein sabse pehle expire hone wali batch dikhao
                //    ONLY for batches that actually have stock (Received - Rejected > 0)
                var earliestBatch = await _context.GRNDetails
                    .Where(g => g.ProductId == item.ProductId && g.ExpDate != null && (g.ReceivedQty - g.RejectedQty) > 0)
                    .OrderBy(g => g.ExpDate)       // Ascending = earliest first
                    .Select(g => new { g.MfgDate, g.ExpDate })
                    .FirstOrDefaultAsync();

                if (earliestBatch != null)
                {
                    item.ManufacturingDate = earliestBatch.MfgDate;
                    item.ExpiryDate = earliestBatch.ExpDate;
                }
                else
                {
                    // Fallback: koi expiry date nahi hai, latest active batch ki date lo
                    var latestBatch = await _context.GRNDetails
                        .Where(g => g.ProductId == item.ProductId && (g.ReceivedQty - g.RejectedQty) > 0)
                        .OrderByDescending(g => g.Id)
                        .Select(g => new { g.MfgDate, g.ExpDate })
                        .FirstOrDefaultAsync();
                    item.ManufacturingDate = latestBatch?.MfgDate;
                    item.ExpiryDate = latestBatch?.ExpDate;
                }

                // 7. Audit Trail History — ReceivedDate UTC se IST (India Standard Time = UTC+5:30) mein convert
                item.History = await _context.GRNDetails
                    .Where(g => g.ProductId == item.ProductId)
                    .OrderByDescending(g => g.GRNHeader.ReceivedDate)
                    .Take(10)
                    .Select(allG => new StockHistoryDto
                    {
                        ProductId = allG.ProductId,
                        WarehouseId = allG.WarehouseId,
                        RackId = allG.RackId,
                        // UTC + 5:30 = IST
                        ReceivedDate = allG.GRNHeader.ReceivedDate.AddHours(5).AddMinutes(30),
                        PONumber = allG.GRNHeader.PurchaseOrder.PoNumber,
                        GRNNumber = allG.GRNHeader.GRNNumber,
                        SupplierName = allG.GRNHeader.PurchaseOrder.SupplierName,
                        ProductName = allG.Product.Name,
                        ReceivedQty = allG.ReceivedQty,
                        RejectedQty = allG.RejectedQty,
                        WarehouseName = allG.Warehouse != null ? allG.Warehouse.Name : "N/A",
                        RackName = allG.Rack != null ? allG.Rack.Name : "N/A",
                        ManufacturingDate = allG.MfgDate,
                        ExpiryDate = allG.ExpDate
                    }).ToListAsync();
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
            // Similar to current stock but focused on Rejected Items (Disposed)
            var baseQuery = _context.GRNDetails.AsNoTracking().AsQueryable();

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
                        RejectedQty = allG.RejectedQty,
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
            var stockData = await _context.GRNDetails
                .Where(x => productIds.Contains(x.ProductId))
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



    }
}
