namespace Inventory.Application.DTOs.SaleOrder;

public class SaleOrderItemGridDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty; // Table "Product" column
    public decimal SoldQty { get; set; } // Table "Sold Qty" column
    public decimal Rate { get; set; } // Table "Rate" column
    public decimal DiscountPercent { get; set; } // Table "Disc %" column
    public decimal TaxPercentage { get; set; } // Table "Tax %" column
    public DateTime? MfgDate { get; set; }
    public DateTime? ExpDate { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? RackId { get; set; }
    public bool IsReturnable { get; set; }
    public double ReturnWindowRemainingHours { get; set; }
}