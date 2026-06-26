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
            var isGlobalAdmin = _currentUserService.IsPlatformAdmin && companyId == Guid.Empty;

            // Base queries with BranchId filtering
            var purchaseOrders = _context.PurchaseOrders.AsNoTracking().AsQueryable();
            var products = _context.Products.AsNoTracking().AsQueryable();
            var saleOrders = _context.SaleOrders.AsNoTracking().AsQueryable();

            if (!isGlobalAdmin)
            {
                purchaseOrders = purchaseOrders.Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
                products = products.Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
                saleOrders = saleOrders.Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
            }

            return new DashboardSummaryDto
            {
                TotalSales = await saleOrders.SumAsync(x => x.GrandTotal),
                PendingPurchaseOrders = await purchaseOrders.CountAsync(x => x.Status == "Submitted"),
                TotalStockItems = (int)(await _context.WarehouseStocks
                    .Where(ws => (isGlobalAdmin || (ws.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || ws.BranchId == branchId))))
                    .SumAsync(x => (decimal?)x.Quantity) ?? 0),
                LowStockAlertCount = await _context.Products
                    .Where(p => (isGlobalAdmin || (p.CompanyId == companyId && p.IsActive && (string.IsNullOrEmpty(branchId) || p.BranchId == branchId || p.BranchId == null))))
                    .CountAsync(p => (_context.WarehouseStocks
                        .Where(ws => ws.ProductId == p.Id && (isGlobalAdmin || (string.IsNullOrEmpty(branchId) || ws.BranchId == branchId)))
                        .Sum(ws => (decimal?)ws.Quantity) ?? 0) <= p.MinStock),
                TotalStockValue = await _context.WarehouseStocks
                    .Where(ws => (isGlobalAdmin || (ws.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || ws.BranchId == branchId))))
                    .SumAsync(ws => ws.Quantity * ws.Product.BasePurchasePrice),
                TotalPurchases = await purchaseOrders.SumAsync(x => x.GrandTotal)
            };
        }

        public async Task<DashboardChartDto> GetDashboardChartsAsync()
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            var isGlobalAdmin = _currentUserService.IsPlatformAdmin && companyId == Guid.Empty;
            var currentYear = DateTime.Now.Year;

            var salesTrendsQuery = _context.SaleOrders.AsNoTracking().Where(x => x.SODate.Year == currentYear);
            if (!isGlobalAdmin)
            {
                salesTrendsQuery = salesTrendsQuery.Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
            }
            var salesTrends = await salesTrendsQuery
                .GroupBy(x => x.SODate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(x => x.GrandTotal) })
                .ToListAsync();

            var purchaseTrendsQuery = _context.PurchaseOrders.AsNoTracking().Where(x => x.PoDate.Year == currentYear);
            if (!isGlobalAdmin)
            {
                purchaseTrendsQuery = purchaseTrendsQuery.Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
            }
            var purchaseTrends = await purchaseTrendsQuery
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

            var rawMaterialsId = await _context.Configurations
                .Where(c => c.ConfigKey == "ProductType" && c.ConfigValue == "Raw Material" && c.IsActive)
                .Select(c => c.Id.ToString())
                .FirstOrDefaultAsync() ?? "14";

            var productTypeIds = await _context.Configurations
                .Where(c => c.ConfigKey == "ProductType" && c.IsActive)
                .Select(c => c.Id.ToString())
                .ToListAsync();

            var finishedGoodsIds = productTypeIds.Where(id => id != rawMaterialsId).ToList();

            chart.FinishedGoods = finishedGoodsIds.Any()
                ? (int)(await _context.WarehouseStocks
                    .Where(ws => ws.CompanyId == companyId 
                        && (string.IsNullOrEmpty(branchId) || ws.BranchId == branchId) 
                        && ws.Product.IsActive 
                        && finishedGoodsIds.Contains(ws.Product.ProductType))
                    .SumAsync(x => (decimal?)x.Quantity) ?? 0)
                : 0;

            chart.RawMaterials = (int)(await _context.WarehouseStocks
                .Where(ws => ws.CompanyId == companyId 
                    && (string.IsNullOrEmpty(branchId) || ws.BranchId == branchId) 
                    && ws.Product.IsActive 
                    && ws.Product.ProductType == rawMaterialsId)
                .SumAsync(x => (decimal?)x.Quantity) ?? 0);

            chart.DamagedItems = (int)await _context.Products
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
                .SumAsync(x => x.DamagedStock);

            var topSellingQuery = _context.SaleOrderItems.AsNoTracking();
            if (!isGlobalAdmin)
            {
                topSellingQuery = topSellingQuery.Where(x => x.CompanyId == companyId && (x.SaleOrder.BranchId == null || string.IsNullOrEmpty(branchId) || x.SaleOrder.BranchId == branchId));
            }

            var topSelling = await topSellingQuery
                .GroupBy(x => x.ProductName)
                .Select(g => new { ProductName = g.Key, TotalQty = g.Sum(x => x.Qty) })
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
            var isGlobalAdmin = _currentUserService.IsPlatformAdmin && companyId == Guid.Empty;

            var salesQuery = _context.SaleOrderItems.AsNoTracking();
            if (!isGlobalAdmin)
            {
                salesQuery = salesQuery.Where(x => x.CompanyId == companyId && (x.SaleOrder.BranchId == null || string.IsNullOrEmpty(branchId) || x.SaleOrder.BranchId == branchId));
            }

            var sales = await salesQuery
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

            var purchasesQuery = _context.PurchaseOrderItems.AsNoTracking();
            if (!isGlobalAdmin)
            {
                purchasesQuery = purchasesQuery.Where(x => x.CompanyId == companyId && (x.PurchaseOrder.BranchId == null || string.IsNullOrEmpty(branchId) || x.PurchaseOrder.BranchId == branchId));
            }

            var purchases = await purchasesQuery
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
