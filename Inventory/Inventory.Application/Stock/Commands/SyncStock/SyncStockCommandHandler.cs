using Inventory.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Stock.Commands
{
    public class SyncStockCommandHandler : IRequestHandler<SyncStockCommand, bool>
    {
        private readonly IInventoryDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public SyncStockCommandHandler(IInventoryDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(SyncStockCommand request, CancellationToken ct)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            
            // 1. CLEAR AND REBUILD WAREHOUSE STOCKS (OR UPDATE)
            // Strategy: Calculate current state from transactions and update WarehouseStocks
            
            // Step 1: Get all transactions grouped by Product and Warehouse
            var grnStock = await _context.GRNDetails
                .IgnoreQueryFilters()
                .Where(g => g.CompanyId == companyId && g.WarehouseId != null)
                .GroupBy(g => new { g.ProductId, g.WarehouseId })
                .Select(g => new { ProductId = (Guid)g.Key.ProductId, WarehouseId = (Guid)g.Key.WarehouseId.Value, Qty = g.Sum(x => x.ReceivedQty - x.RejectedQty) })
                .ToListAsync(ct);

            var saleStock = await _context.SaleOrderItems
                .IgnoreQueryFilters()
                .Where(s => s.CompanyId == companyId && s.WarehouseId != null && 
                            s.SaleOrder.Status != "Draft" && s.SaleOrder.Status != "Cancelled" && s.SaleOrder.Status != "Canceled")
                .GroupBy(s => new { s.ProductId, s.WarehouseId })
                .Select(s => new { ProductId = (Guid)s.Key.ProductId, WarehouseId = (Guid)s.Key.WarehouseId.Value, Qty = s.Sum(x => x.Qty) })
                .ToListAsync(ct);

            var saleReturnStock = await _context.SaleReturnItems
                .IgnoreQueryFilters()
                .Where(sr => sr.CompanyId == companyId && sr.WarehouseId != null
                    && (sr.SaleReturnHeader.Status == "Confirmed" || sr.SaleReturnHeader.Status == "INWARDED"))
                .GroupBy(sr => new { sr.ProductId, sr.WarehouseId })
                .Select(sr => new { ProductId = (Guid)sr.Key.ProductId, WarehouseId = (Guid)sr.Key.WarehouseId.Value, Qty = sr.Sum(x => x.ReturnQty) })
                .ToListAsync(ct);

            var purchaseReturnStock = await _context.PurchaseReturnItems
                .IgnoreQueryFilters()
                .Where(pr => pr.CompanyId == companyId && pr.WarehouseId != null)
                .GroupBy(pr => new { pr.ProductId, pr.WarehouseId })
                .Select(pr => new { ProductId = (Guid)pr.Key.ProductId, WarehouseId = (Guid)pr.Key.WarehouseId.Value, Qty = pr.Sum(x => x.ReturnQty) })
                .ToListAsync(ct);

            var transferOutStock = await _context.StockTransferDetails
                .IgnoreQueryFilters()
                .Where(t => t.CompanyId == companyId && t.StockTransferHeader.Status == "Completed" && t.StockTransferHeader.FromWarehouseId != null)
                .GroupBy(t => new { t.ProductId, WarehouseId = t.StockTransferHeader.FromWarehouseId })
                .Select(t => new { ProductId = t.Key.ProductId, WarehouseId = t.Key.WarehouseId, Qty = t.Sum(x => x.Quantity) })
                .ToListAsync(ct);

            var transferInStock = await _context.StockTransferDetails
                .IgnoreQueryFilters()
                .Where(t => t.CompanyId == companyId && t.StockTransferHeader.Status == "Completed" && t.StockTransferHeader.ToWarehouseId != null)
                .GroupBy(t => new { t.ProductId, WarehouseId = t.StockTransferHeader.ToWarehouseId })
                .Select(t => new { ProductId = t.Key.ProductId, WarehouseId = t.Key.WarehouseId, Qty = t.Sum(x => x.Quantity) })
                .ToListAsync(ct);

            // Step 2: Combine all into a dictionary for processing
            // Key: ProductId_WarehouseId
            var stockMap = new Dictionary<string, decimal>();

            foreach (var item in grnStock) stockMap[$"{item.ProductId}_{item.WarehouseId}"] = item.Qty;
            
            foreach (var item in saleStock) 
            {
                var key = $"{item.ProductId}_{item.WarehouseId}";
                if (stockMap.ContainsKey(key)) stockMap[key] -= item.Qty;
                else stockMap[key] = -item.Qty;
            }

            foreach (var item in saleReturnStock)
            {
                var key = $"{item.ProductId}_{item.WarehouseId}";
                if (stockMap.ContainsKey(key)) stockMap[key] += item.Qty;
                else stockMap[key] = item.Qty;
            }

            foreach (var item in purchaseReturnStock)
            {
                var key = $"{item.ProductId}_{item.WarehouseId}";
                if (stockMap.ContainsKey(key)) stockMap[key] -= item.Qty;
                else stockMap[key] = -item.Qty;
            }

            foreach (var item in transferOutStock)
            {
                var key = $"{item.ProductId}_{item.WarehouseId}";
                if (stockMap.ContainsKey(key)) stockMap[key] -= item.Qty;
                else stockMap[key] = -item.Qty;
            }

            foreach (var item in transferInStock)
            {
                var key = $"{item.ProductId}_{item.WarehouseId}";
                if (stockMap.ContainsKey(key)) stockMap[key] += item.Qty;
                else stockMap[key] = item.Qty;
            }

            // Step 3: Update WarehouseStocks Table
            var existingWhStocks = await _context.WarehouseStocks.IgnoreQueryFilters().Where(ws => ws.CompanyId == companyId).ToListAsync(ct);
            
            foreach (var entry in stockMap)
            {
                var parts = entry.Key.Split('_');
                var productId = Guid.Parse(parts[0]);
                var warehouseId = Guid.Parse(parts[1]);
                var finalQty = entry.Value;

                var existing = existingWhStocks.FirstOrDefault(ws => ws.ProductId == productId && ws.WarehouseId == warehouseId);
                if (existing != null)
                {
                    existing.Quantity = finalQty;
                }
                else
                {
                    // Fetch branchId from Warehouse table to keep data consistent
                    var warehouse = await _context.Warehouses.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(w => w.Id == warehouseId);
                    
                    await _context.WarehouseStocks.AddAsync(new Inventory.Domain.Entities.WarehouseStock
                    {
                        ProductId = productId,
                        WarehouseId = warehouseId,
                        Quantity = finalQty,
                        CompanyId = companyId,
                        BranchId = warehouse?.BranchId,
                        MinStock = 0
                    });
                }
            }

            // Step 4: RECONCILE PURCHASE ORDER QUANTITIES
            // Recalculate ReceivedQty in PO Items based on GRNs and Returns
            var poItems = await _context.PurchaseOrderItems.Where(poi => poi.CompanyId == companyId).ToListAsync(ct);
            foreach (var item in poItems)
            {
                var acceptedInGrn = await _context.GRNDetails
                    .Where(gd => gd.ProductId == item.ProductId && gd.GRNHeader.PurchaseOrderId == item.PurchaseOrderId && gd.CompanyId == companyId)
                    .SumAsync(gd => gd.ReceivedQty - gd.RejectedQty, ct);

                var returnedInPr = await _context.PurchaseReturnItems
                    .Where(ri => ri.ProductId == item.ProductId && ri.CompanyId == companyId)
                    .Join(_context.GRNDetails.Where(gd => gd.GRNHeader.PurchaseOrderId == item.PurchaseOrderId),
                          ri => ri.GrnRef, gd => gd.GRNHeader.GRNNumber, (ri, gd) => (decimal?)ri.ReturnQty)
                    .SumAsync(ct) ?? 0;

                item.ReceivedQty = acceptedInGrn - returnedInPr;
                if (item.ReceivedQty < 0) item.ReceivedQty = 0;
            }
            
            return await _context.SaveChangesAsync(ct) > 0;
        }
    }
}
