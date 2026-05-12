using Inventory.Application.Clients;
using Inventory.Application.SaleOrders.DTOs;
using Inventory.Application.SaleOrders.SaleReturn.DTOs;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Inventory.Application.Common.Interfaces;

namespace Inventory.Infrastructure.Repositories
{
    public class SaleReturnRepository : Inventory.Application.Common.Interfaces.ISaleReturnRepository
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly InventoryDbContext _context;
        private readonly ICustomerClient _customerClient;

        public SaleReturnRepository(InventoryDbContext context, ICustomerClient customerClient, ICurrentUserService currentUserService
            )
        {
            _context = context;
            _customerClient = customerClient;
            _currentUserService = currentUserService;
        }

        public async Task<SaleReturnPagedResponse> GetSaleReturnsAsync(
         string? search,
         string? status,
         int pageIndex,
         int pageSize,
         DateTime? fromDate,
         DateTime? toDate,
         string sortField,
         string sortOrder,
         bool isQuick = false)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            // 1. Initial Query with NoTracking for high performance
            var query = _context.SaleReturnHeaders
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.IsQuick == isQuick && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
                .Include(x => x.SaleOrder) // Join once for SO Ref
                .AsQueryable();

            // 2. Date filtering (Include entire end date)
            if (fromDate.HasValue)
                query = query.Where(x => x.ReturnDate >= fromDate.Value);

            if (toDate.HasValue)
            {
                var endOfToDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.ReturnDate <= endOfToDate);
            }

            // 3. Optimized Status Widget Filter
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "TODAY")
                {
                    var today = DateTime.Today;
                    var tomorrow = today.AddDays(1);
                    query = query.Where(x => x.ReturnDate >= today && x.ReturnDate < tomorrow);
                }
                else
                {
                    query = query.Where(x => x.Status == status);
                }
            }

            // 4. Robust Searching Logic
            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower().Trim();
                // Step: Fetch matching IDs from Customer Microservice
                var matchingCustomerIds = await _customerClient.SearchCustomerIdsByNameAsync(s);

                query = query.Where(x => 
                    x.ReturnNumber.ToLower().Contains(s) ||
                    (x.SaleOrder != null && x.SaleOrder.SONumber.ToLower().Contains(s)) ||
                    (matchingCustomerIds != null && matchingCustomerIds.Contains(x.CustomerId)));
            }

            // 5. SERVER-SIDE SORTING (Default: CreatedOn DESC)
            bool isDesc = sortOrder?.ToLower() == "desc" || string.IsNullOrEmpty(sortOrder);
            string effectiveSortField = (sortField ?? "").ToLower().Trim() switch
            {
                "returnnumber" => "ReturnNumber",
                "returndate" => "ReturnDate",
                "totalamount" => "TotalAmount",
                "status" => "Status",
                "soref" => "SaleOrder.SONumber",
                "customername" => "CustomerId", // Proxy sort by ID for remote names
                "createdon" => "CreatedOn",
                "id" => "Id",
                _ => "CreatedOn" // Default newest record first
            };

            if (isDesc)
                query = query.OrderByDescending(x => EF.Property<object>(x, effectiveSortField));
            else
                query = query.OrderBy(x => EF.Property<object>(x, effectiveSortField));

            // 6. Fast Server-Side Count
            var totalCount = await query.CountAsync();

            // 7. Execution & Pagination
            var pagedData = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .Select(x => new SaleReturnListDto
                {
                    SaleReturnHeaderId = x.Id,
                    ReturnNumber = x.ReturnNumber,
                    ReturnDate = x.ReturnDate,
                    CustomerId = x.CustomerId,
                    SoRef = x.SaleOrder != null ? x.SaleOrder.SONumber : string.Empty,
                    TotalAmount = x.TotalAmount,
                    Status = x.Status,
                    GatePassNo = x.GatePassNo,
                    IsQuick = x.IsQuick
                }).ToListAsync();

            if (pagedData == null || !pagedData.Any())
                return new SaleReturnPagedResponse { Items = new List<SaleReturnListDto>(), TotalCount = totalCount };

            // Fetch Item details (ProductName and TotalQty)
            var pagedIds = pagedData.Select(x => x.SaleReturnHeaderId).ToList();
            var returnItemsList = await (from ri in _context.SaleReturnItems.AsNoTracking()
                                         join p in _context.Products.AsNoTracking() on ri.ProductId equals p.Id
                                         where pagedIds.Contains(ri.SaleReturnHeaderId) && ri.CompanyId == companyId
                                         select new { ri.SaleReturnHeaderId, ri.ReturnQty, ProductName = p.Name })
                                         .ToListAsync();

            var itemLookup = returnItemsList
                .GroupBy(x => x.SaleReturnHeaderId)
                .ToDictionary(g => g.Key, g => new {
                    TotalQty = g.Sum(i => i.ReturnQty),
                    ProductName = g.Count() == 1 ? g.First().ProductName : (g.Count() > 1 ? "Multiple Items" : "N/A")
                });

            // 8. Bulk Customer Name Enrichment & Item Mapping
            var customerIds = pagedData.Select(i => i.CustomerId).Distinct().ToList();
            var customerMap = customerIds.Any() ? await _customerClient.GetCustomerNamesAsync(customerIds) : new Dictionary<Guid, string>();

            foreach (var item in pagedData)
            {
                // Customer Name
                item.CustomerName = customerMap != null && customerMap.ContainsKey(item.CustomerId)
                                    ? customerMap[item.CustomerId]
                                    : "Unknown Customer";
                
                // Product Info
                if (itemLookup.ContainsKey(item.SaleReturnHeaderId))
                {
                    item.ProductName = itemLookup[item.SaleReturnHeaderId].ProductName;
                    item.TotalQty = itemLookup[item.SaleReturnHeaderId].TotalQty;
                }
                else
                {
                    item.ProductName = "N/A";
                    item.TotalQty = 0;
                }
            }

            return new SaleReturnPagedResponse { Items = pagedData, TotalCount = totalCount };
        }



        public async Task<bool> CreateSaleReturnAsync(SaleReturnHeader header)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {

                    decimal calculatedSubTotal = 0;
                    decimal calculatedTaxAmount = 0;


                    foreach (var item in header.ReturnItems)
                    {
                        var companyId = header.CompanyId;
                        var branchId = header.BranchId;

                        // Auto-resolve branchId from Warehouse if null (for Admin/Cross-branch sessions) [cite: 2026-04-27]
                        if (string.IsNullOrEmpty(branchId) && item.WarehouseId.HasValue)
                        {
                            var warehouse = await _context.Warehouses.AsNoTracking()
                                .FirstOrDefaultAsync(w => w.Id == item.WarehouseId && w.CompanyId == companyId);
                            if (warehouse != null)
                            {
                                branchId = warehouse.BranchId;
                                if (string.IsNullOrEmpty(header.BranchId)) header.BranchId = branchId;
                            }
                        }
                        if (string.IsNullOrEmpty(item.BranchId)) item.BranchId = branchId;

                        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId && p.CompanyId == companyId);
                        if (product != null)
                        {
                            // ⚡ REDUNDANT: Products.CurrentStock removed.
                            product.ModifiedOn = DateTime.Now;
                            product.ModifiedBy = header.CreatedBy ?? "system";

                            // 🚀 UPDATE WAREHOUSE SPECIFIC STOCK (PLUS)
                            if (item.WarehouseId.HasValue && item.WarehouseId != Guid.Empty)
                            {
                                var whStock = await _context.WarehouseStocks
                                    .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == item.WarehouseId);

                                if (whStock != null)
                                {
                                    whStock.Quantity += item.ReturnQty;
                                }
                                else
                                {
                                    await _context.WarehouseStocks.AddAsync(new WarehouseStock
                                    {
                                        ProductId = item.ProductId,
                                        WarehouseId = item.WarehouseId.Value,
                                        Quantity = item.ReturnQty,
                                        MinStock = 0,
                                        CompanyId = companyId,
                                        BranchId = branchId
                                    });
                                }
                            }


                            // 🆕 Record Inventory Transaction
                            var returnTx = new InventoryTransaction(
                                item.ProductId,
                                item.ReturnQty,
                                header.IsQuick ? "QuickSaleReturn" : "SaleReturn",
                                header.ReturnNumber,
                                item.WarehouseId, 
                                item.RackId,
                                item.MfgDate,
                                item.ExpDate,
                                companyId,
                                branchId,
                                item.ReferenceNumber,
                                item.BatchNumber
                            );
                            await _context.InventoryTransactions.AddAsync(returnTx);
                        }


                        // Remove tax-exclusive recalculations because Handler has already calculated everything using the proper tax-inclusive formula from the frontend.
                        calculatedSubTotal += (item.TotalAmount - item.TaxAmount);
                        calculatedTaxAmount += item.TaxAmount;
                    }

                    // 3. Header table columns update
                    header.SubTotal = calculatedSubTotal;
                    header.TaxAmount = calculatedTaxAmount;
                    header.TotalAmount = header.ReturnItems.Sum(i => i.TotalAmount); // Final sync
                    header.CreatedOn = DateTime.Now;

                    // 4. Save Sale Return
                    _context.SaleReturnHeaders.Add(header);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            });
        }

        public async Task<decimal> GetRemainingReturnableQtyAsync(Guid saleOrderId, Guid productId, DateTime? mfgDate = null, DateTime? expDate = null)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            // 1. Get total quantity sold for THIS specifically batch-matched line item
            var totalSold = await _context.SaleOrderItems
                .AsNoTracking()
                .Where(soi => soi.SaleOrderId == saleOrderId &&
                              soi.ProductId == productId &&
                              soi.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || soi.BranchId == branchId) &&
                              (!mfgDate.HasValue || soi.MfgDate == mfgDate) &&
                              (!expDate.HasValue || soi.ExpDate == expDate))
                .SumAsync(soi => (decimal?)soi.Qty) ?? 0;


            // 2. Get total already returned for THIS specific batch
            var totalReturned = await _context.SaleReturnItems
                .AsNoTracking()
                .Where(sri => sri.SaleReturnHeader.SaleOrderId == saleOrderId &&
                              sri.ProductId == productId &&
                              sri.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || sri.BranchId == branchId) &&
                              (sri.SaleReturnHeader.Status == "Confirmed" || sri.SaleReturnHeader.Status == "INWARDED") &&
                              (!mfgDate.HasValue || sri.MfgDate == mfgDate) &&
                              (!expDate.HasValue || sri.ExpDate == expDate))
                .SumAsync(sri => (decimal?)sri.ReturnQty) ?? 0;

            var remaining = totalSold - totalReturned;
            return remaining > 0 ? remaining : 0;
        }

        public async Task<List<SaleReturnExportDto>> GetExportDataAsync(DateTime? fromDate, DateTime? toDate)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            return await _context.SaleReturnHeaders
                .AsNoTracking()
                .Include(h => h.SaleOrder) // Join for SONumber
                .Where(h => h.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || h.BranchId == branchId) && (!fromDate.HasValue || h.ReturnDate >= fromDate) &&
                            (!toDate.HasValue || h.ReturnDate <= toDate))
                .Select(h => new SaleReturnExportDto
                {
                    ReturnNumber = h.ReturnNumber,
                    ReturnDate = h.ReturnDate.ToString("dd-MM-yyyy"),
                    SONumber = h.SaleOrder.SONumber ?? "N/A", // From SaleOrders table
                    TotalAmount = h.TotalAmount, //
                    Status = h.Status,
                    IsQuick = h.IsQuick,
                    CustomerName = h.CustomerId.ToString()
                })
                .ToListAsync();
        }
        
        public async Task<SaleReturnSummaryDto> GetDashboardSummaryAsync(bool isQuick = false)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            var queryBase = _context.SaleReturnHeaders.Where(x => x.IsQuick == isQuick && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId));

            // 1. Aaj kitne returns aaye (Range check is safer than .Date)
            var totalToday = await queryBase
                .CountAsync(x => x.ReturnDate >= today && x.ReturnDate < tomorrow);

            // 2. Confirmed/Inwarded returns ka count aur refund value (DB Side Aggregation)
            var confirmedQuery = queryBase
                .Where(x => x.Status.ToUpper() == "CONFIRMED" || x.Status.ToUpper() == "INWARDED");

            var totalRefundValue = await confirmedQuery.SumAsync(x => (decimal?)x.TotalAmount) ?? 0;
            var confirmedCount = await confirmedQuery.CountAsync();

            // 3. Pending Inward Count (Confirmed but no GatePassNo) - Module specific
            var pendingInwardCount = await queryBase
                .CountAsync(x => (x.Status.ToUpper() == "CONFIRMED" || x.Status.ToUpper() == "INWARDED") && (x.GatePassNo == null || x.GatePassNo == ""));

            // 4. Stock re-filled pcs (Items table se sum)
            var totalPcs = await _context.SaleReturnItems
                .Where(x => x.CompanyId == companyId && x.SaleReturnHeader.IsQuick == isQuick && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
                .SumAsync(x => (decimal?)x.ReturnQty) ?? 0;

            return new SaleReturnSummaryDto
            {
                TotalReturnsToday = totalToday,
                TotalRefundValue = totalRefundValue,
                ConfirmedReturns = confirmedCount,
                PendingInwardCount = pendingInwardCount,
                StockRefilledPcs = totalPcs
            };
        }

        public async Task<List<PendingSRDto>> GetPendingSaleReturnsAsync()
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            var returns = await _context.SaleReturnHeaders
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.Status == "Confirmed" && (x.GatePassNo == null || x.GatePassNo == "") && x.IsQuick == false && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
                .OrderByDescending(x => x.ReturnDate)
                .Select(x => new PendingSRDto
                {
                    Id = x.Id,
                    ReturnNumber = x.ReturnNumber,
                    ReturnDate = x.ReturnDate,
                    Status = x.Status,
                    // Note: We'll add CustomerId to DTO or use it from projection
                    TotalQty = x.ReturnItems.Sum(i => i.ReturnQty)
                })
                .ToListAsync();

            if (returns == null || !returns.Any()) return new List<PendingSRDto>();

            // For customer names, we need CustomerId which is in the entity but not in DTO yet.
            // Let's re-query with CustomerId or modify projection.
            
            var detailedReturns = await _context.SaleReturnHeaders
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.Status == "Confirmed" && (x.GatePassNo == null || x.GatePassNo == "") && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
                .OrderByDescending(x => x.ReturnDate)
                .Select(x => new { x.Id, x.CustomerId })
                .ToListAsync();

            var customerIds = detailedReturns.Select(x => x.CustomerId).Distinct().ToList();
            var customerMap = await _customerClient.GetCustomerNamesAsync(customerIds);

            foreach (var r in returns)
            {
                var original = detailedReturns.First(x => x.Id == r.Id);
                r.CustomerName = customerMap != null && customerMap.ContainsKey(original.CustomerId) 
                                 ? customerMap[original.CustomerId] 
                                 : "Unknown Customer";
            }

            return returns;
        }

        public async Task<bool> BulkInwardAsync(List<Guid> ids)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            var records = await _context.SaleReturnHeaders
                .Where(x => ids.Contains(x.Id) && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
                .ToListAsync();

            if (!records.Any()) return false;

            bool changed = false;
            foreach (var record in records)
            {
                if (record.Status != "INWARDED")
                {
                    record.Status = "INWARDED";
                    changed = true;
                }
            }

            if (changed)
            {
                await _context.SaveChangesAsync();
            }
            return true;
        }

        public async Task<SaleReturnHeader?> GetSaleReturnByIdAsync(Guid id)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            return await _context.SaleReturnHeaders
                .Include(x => x.ReturnItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
        }
    }
}
