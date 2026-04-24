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
            
            var products = _context.Products.AsNoTracking()
                .Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
            
            var saleOrders = _context.SaleOrders.AsNoTracking()
                .Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId));

            return new DashboardSummaryDto
            {
                TotalSales = await saleOrders.SumAsync(x => x.GrandTotal),
                PendingPurchaseOrders = await purchaseOrders.CountAsync(x => x.Status == "Submitted"),
                TotalStockItems = (int)await products.SumAsync(x => x.CurrentStock),
                LowStockAlertCount = await products.CountAsync(x => x.IsActive && x.CurrentStock <= x.MinStock),
                TotalStockValue = await products
                    .Where(x => x.IsActive)
                    .SumAsync(x => x.CurrentStock * x.BasePurchasePrice)
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

            chart.FinishedGoods = (int)await _context.Products
                .AsNoTracking()
                .Where(x => x.IsActive && x.ProductType == "1" && x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
                .SumAsync(x => x.CurrentStock);

            chart.RawMaterials = (int)await _context.Products
                .AsNoTracking()
                .Where(x => x.IsActive && x.ProductType == "2" && x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
                .SumAsync(x => x.CurrentStock);

            chart.DamagedItems = (int)await _context.Products
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId && (x.BranchId == null || string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
                .SumAsync(x => x.DamagedStock);

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
