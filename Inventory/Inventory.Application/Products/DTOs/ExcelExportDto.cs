namespace Inventory.Application.Products.DTOs;

public class ExcelExportDto
{
    public string ProductName { get; set; }
    public string SKU { get; set; }
    public string Category { get; set; }
    public decimal CurrentStock { get; set; }
    public int MinStock { get; set; }
    public decimal Discount { get; set; }
    public string Unit { get; set; }
    public string Warehouse { get; set; }
    public string Rack { get; set; }
    public bool IsExpiryRequired { get; set; }
}
