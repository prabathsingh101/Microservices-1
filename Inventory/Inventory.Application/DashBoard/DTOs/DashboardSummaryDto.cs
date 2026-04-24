namespace Inventory.Application.DashBoard.DTOs;

public class DashboardSummaryDto
{
    public decimal TotalSales { get; set; } // ?4,50,000
    public int PendingPurchaseOrders { get; set; } // 24 Pending
    public int TotalStockItems { get; set; } // 1,240 Units
    public int LowStockAlertCount { get; set; } // 5 Items
    public decimal TotalStockValue { get; set; }
 
}
