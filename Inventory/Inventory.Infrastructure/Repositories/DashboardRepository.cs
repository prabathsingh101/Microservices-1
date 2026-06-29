using Inventory.Application.Common.Interfaces;
using Inventory.Application.DashBoard.DTOs;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly InventoryDbContext _context;

        public DashboardRepository(InventoryDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;

            // Base queries with BranchId filtering
            var purchaseOrders = _context.PurchaseOrders.AsNoTracking()
                .Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
            
            var saleOrders = _context.SaleOrders.AsNoTracking()
                .Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId));

            // 🚀 Optimized live stock aggregate lookup
            var warehouseStocksSum = await _context.WarehouseStocks
                .AsNoTracking()
                .Where(ws => ws.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || ws.BranchId == branchId))
                .GroupBy(ws => ws.ProductId)
                .Select(g => new { ProductId = g.Key, TotalQty = g.Sum(x => (decimal?)x.Quantity) ?? 0 })
                .ToDictionaryAsync(x => x.ProductId, x => x.TotalQty);

            var activeProducts = await _context.Products
                .AsNoTracking()
                .Where(p => p.CompanyId == companyId && p.IsActive && (string.IsNullOrEmpty(branchId) || p.BranchId == branchId || p.BranchId == null))
                .Select(p => new { p.Id, p.MinStock, p.BasePurchasePrice })
                .ToListAsync();

            var lowStockCount = activeProducts.Count(p => warehouseStocksSum.GetValueOrDefault(p.Id, 0) <= p.MinStock);
            var totalStockItems = (int)warehouseStocksSum.Values.Sum();
            var totalStockValue = activeProducts.Sum(p => warehouseStocksSum.GetValueOrDefault(p.Id, 0) * p.BasePurchasePrice);

            return new DashboardSummaryDto
            {
                TotalSales = await saleOrders.SumAsync(x => x.GrandTotal),
                PendingPurchaseOrders = await purchaseOrders.CountAsync(x => x.Status == "Submitted"),
                TotalStockItems = totalStockItems,
                LowStockAlertCount = lowStockCount,
                TotalStockValue = totalStockValue
            };
        }

        public async Task<DashboardChartDto> GetDashboardChartsAsync()
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            var currentYear = DateTime.Now.Year;

            var salesTrends = await _context.SaleOrders
                .AsNoTracking()
                .Where(x => x.SODate.Year == currentYear && x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
                .GroupBy(x => x.SODate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(x => x.GrandTotal) })
                .ToListAsync();

            var purchaseTrends = await _context.PurchaseOrders
                .AsNoTracking()
                .Where(x => x.PoDate.Year == currentYear && x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
                .GroupBy(x => x.PoDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(x => x.GrandTotal) })
                .ToListAsync();

            var chart = new DashboardChartDto();
            for (int i = 1; i <= 7; i++)
            {
                string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(i);
                chart.Labels.Add(monthName);
                chart.SalesData.Add(salesTrends.FirstOrDefault(x => x.Month == i)?.Total ?? 0);
                chart.PurchaseData.Add(purchaseTrends.FirstOrDefault(x => x.Month == i)?.Total ?? 0);
            }

            // 🎯 1. Load active configurations for ProductType to resolve ID string to display labels
            var productTypes = await _context.Configurations
                .AsNoTracking()
                .Where(c => c.ConfigKey == "ProductType" && c.IsActive)
                .ToDictionaryAsync(c => c.Id.ToString(), c => c.ConfigValue);

            // 🎯 2. Query stocks grouped by ProductType using optimized AsNoTracking query
            var stockByType = await _context.WarehouseStocks
                .AsNoTracking()
                .Where(ws => ws.CompanyId == companyId 
                    && (string.IsNullOrEmpty(branchId) || ws.BranchId == branchId)
                    && ws.Product.IsActive)
                .GroupBy(ws => ws.Product.ProductType)
                .Select(g => new { ProductType = g.Key, TotalQty = g.Sum(x => (decimal?)x.Quantity) ?? 0 })
                .ToListAsync();

            var labelMap = new Dictionary<string, decimal>();
            foreach (var item in stockByType)
            {
                var typeId = (item.ProductType ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(typeId)) continue;

                // Resolve type label, fallback to ID or "Unknown"
                var label = productTypes.TryGetValue(typeId, out var val) ? val : typeId;
                if (label.Equals("goods", StringComparison.OrdinalIgnoreCase))
                {
                    label = "Finished Goods"; // standard label representation
                }

                if (labelMap.ContainsKey(label))
                {
                    labelMap[label] += item.TotalQty;
                }
                else
                {
                    labelMap[label] = item.TotalQty;
                }
            }

            // 🎯 3. Damaged stock summation
            var damagedSum = await _context.Products
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
                .SumAsync(x => x.DamagedStock);

            if (damagedSum > 0)
            {
                labelMap["Damaged"] = damagedSum;
            }

            chart.StockStatusLabels = labelMap.Keys.ToList();
            chart.StockStatusValues = labelMap.Values.ToList();

            // Keep legacy properties populated for backward compatibility (where ProductType is "1" or "2")
            chart.FinishedGoods = (int)(labelMap.TryGetValue("Finished Goods", out var fg) ? fg : (labelMap.TryGetValue("Finished", out var f) ? f : (labelMap.TryGetValue("Goods", out var g) ? g : 0)));
            chart.RawMaterials = (int)(labelMap.TryGetValue("Raw Material", out var rm) ? rm : (labelMap.TryGetValue("Raw Materials", out var rms) ? rms : 0));
            chart.DamagedItems = (int)damagedSum;

            // 🎯 4. Most Selling Items for the current tenant
            var topSelling = await _context.SaleOrderItems
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId && (x.SaleOrder.BranchId == null || string.IsNullOrEmpty(branchId) || x.SaleOrder.BranchId == branchId))
                .GroupBy(x => x.ProductName)
                .Select(g => new
                {
                    ProductName = g.Key,
                    TotalQty = g.Sum(x => x.Qty)
                })
                .OrderByDescending(x => x.TotalQty)
                .Take(5)
                .ToListAsync();

            chart.TopSellingProducts = topSelling.Select(x => x.ProductName ?? "Unknown").ToList();
            chart.TopSellingQtys = topSelling.Select(x => x.TotalQty).ToList();

            return chart;
        }

        public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync()
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;

            var sales = await _context.SaleOrderItems
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId && (x.SaleOrder.BranchId == null || string.IsNullOrEmpty(branchId) || x.SaleOrder.BranchId == branchId))
                .OrderByDescending(x => x.SaleOrder.SODate)
                .Take(5)
                .Select(x => new RecentActivityDto
                {
                    Product = x.ProductName,
                    Type = "Sale",
                    Qty = x.Qty,
                    Date = x.SaleOrder.SODate,
                    Status = x.SaleOrder.Status
                }).ToListAsync();

            var purchases = await _context.PurchaseOrderItems
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId && (x.PurchaseOrder.BranchId == null || string.IsNullOrEmpty(branchId) || x.PurchaseOrder.BranchId == branchId))
                .OrderByDescending(x => x.PurchaseOrder.PoDate)
                .Take(5)
                .Select(x => new RecentActivityDto
                {
                    Product = x.Product != null ? x.Product.Name : "Unknown",
                    Type = "Purchase",
                    Qty = x.Qty,
                    Date = x.PurchaseOrder.PoDate,
                    Status = x.PurchaseOrder.Status
                }).ToListAsync();

            return sales.Concat(purchases)
                .OrderByDescending(x => x.Date)
                .Take(5)
                .ToList();
        }
    }
}
