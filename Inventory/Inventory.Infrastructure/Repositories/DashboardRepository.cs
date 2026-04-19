using Inventory.Application.Common.Interfaces;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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
        // AsNoTracking read-only operations ke liye fast hai
        var purchaseOrders = _context.PurchaseOrders.AsNoTracking().Where(x => x.CompanyId == companyId);
        var products = _context.Products.AsNoTracking().Where(x => x.CompanyId == companyId);
        var saleOrders = _context.SaleOrders.AsNoTracking().Where(x => x.CompanyId == companyId);

        return new DashboardSummaryDto
        {
            // 1. Total Sales: SaleOrders table ke GrandTotal column ka sum
            TotalSales = await saleOrders.SumAsync(x => x.GrandTotal),

            // 2. FIX: Dashboard par Manager ko approval ke liye "Submitted" orders dikhne chahiye
            // Status "Submitted" matlab user ne bhej diya hai aur Manager ke Approval ka wait hai
            PendingPurchaseOrders = await purchaseOrders.CountAsync(x => x.Status == "Submitted"),

            // 3. Total Stock Units: Products table ke CurrentStock column ka sum
            TotalStockItems = (int)await products.SumAsync(x => x.CurrentStock),

            // 4. Low Stock Alert: CurrentStock jab MinStock se kam ya barabar ho
            LowStockAlertCount = await products.CountAsync(x => x.IsActive && x.CurrentStock <= x.MinStock),

            // 5. Total Stock Value: (CurrentStock * BasePurchasePrice)
            // Isse warehouse mein pade total maal ki keemat pata chalti hai
            TotalStockValue = await products
                .Where(x => x.IsActive)
                .SumAsync(x => x.CurrentStock * x.BasePurchasePrice)
        };
    }

    public async Task<DashboardChartDto> GetDashboardChartsAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var currentYear = DateTime.Now.Year;

        // 1. Sales Trends: SODate ke basis par month-wise group karke GrandTotal sum karna
        var salesTrends = await _context.SaleOrders
            .AsNoTracking()
            .Where(x => x.SODate.Year == currentYear && x.CompanyId == companyId)
            .GroupBy(x => x.SODate.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(x => x.GrandTotal) })
            .ToListAsync();

        // 2. Purchase Trends: PoDate ke basis par month-wise group karke GrandTotal sum karna
        var purchaseTrends = await _context.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.PoDate.Year == currentYear && x.CompanyId == companyId)
            .GroupBy(x => x.PoDate.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(x => x.GrandTotal) })
            .ToListAsync();

        var chart = new DashboardChartDto();

        // Jan se July tak ke labels aur real data mapping
        for (int i = 1; i <= 7; i++)
        {
            string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(i);
            chart.Labels.Add(monthName);

            chart.SalesData.Add(salesTrends.FirstOrDefault(x => x.Month == i)?.Total ?? 0);
            chart.PurchaseData.Add(purchaseTrends.FirstOrDefault(x => x.Month == i)?.Total ?? 0);
        }

        // 3. Stock Distribution (Donut Chart) - Updated with new columns

        // Finished Goods: Jahan ProductType 1 hai (Maan lijiye 1 = Finished) aur active hai
        chart.FinishedGoods = (int)await _context.Products
            .AsNoTracking()
            .Where(x => x.IsActive && x.ProductType == "1" && x.CompanyId == companyId)
            .SumAsync(x => x.CurrentStock);

        // Raw Material: Jahan ProductType 2 hai (Maan lijiye 2 = Raw Material)
        chart.RawMaterials = (int)await _context.Products
            .AsNoTracking()
            .Where(x => x.IsActive && x.ProductType == "2" && x.CompanyId == companyId)
            .SumAsync(x => x.CurrentStock);

        // Damaged Items: Naye DamagedStock column ka total sum
        chart.DamagedItems = (int)await _context.Products
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .SumAsync(x => x.DamagedStock);

        return chart;
    }

    public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        // 1. Sales Transactions (Using SaleOrders + SaleOrderItems)
        var sales = await _context.SaleOrderItems
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.SaleOrder.SODate)
            .Take(5)
            .Select(x => new RecentActivityDto
            {
                Product = x.ProductName, // SaleOrderItems mein ProductName direct hai
                Type = "Sale",
                Qty = x.Qty,
                Date = x.SaleOrder.SODate,
                Status = x.SaleOrder.Status
            }).ToListAsync();

        // 2. Purchase Transactions (Using PurchaseOrders + PurchaseOrderItems + Products)
        var purchases = await _context.PurchaseOrderItems
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.PurchaseOrder.PoDate) // Ab ye navigation property kaam karegi
            .Take(5)
            .Select(x => new RecentActivityDto
            {
                // Product table se Name lene ke liye direct navigation use karein
                Product = x.Product != null ? x.Product.Name : "Unknown",
                Type = "Purchase",
                Qty = x.Qty,
                Date = x.PurchaseOrder.PoDate,
                Status = x.PurchaseOrder.Status
            }).ToListAsync();

        // 3. Dono ko combine karke dashboard ke liye final Top 5 transactions nikalna
        return sales.Concat(purchases)
            .OrderByDescending(x => x.Date)
            .Take(5)
            .ToList();
    }
}
