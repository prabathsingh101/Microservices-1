namespace Inventory.Application.DashBoard.DTOs;

public class DashboardChartDto
{
    // Trends Chart (Line Chart)
    public List<string> Labels { get; set; } = new(); // Jan, Feb...
    public List<decimal> SalesData { get; set; } = new(); // Green Line
    public List<decimal> PurchaseData { get; set; } = new(); // Blue Line

    // Stock Status (Donut Chart)
    public int FinishedGoods { get; set; }
    public int RawMaterials { get; set; }
    public int DamagedItems { get; set; }
}
