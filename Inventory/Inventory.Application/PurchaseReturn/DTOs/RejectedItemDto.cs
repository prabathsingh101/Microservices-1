public class RejectedItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public string GrnRef { get; set; } // e.g., PO/26-27/0004
    public decimal RejectedQty { get; set; } // Available for return
    public decimal Rate { get; set; }
    public decimal GstPercent { get; set; }
    public decimal DiscountPercent { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? RackId { get; set; }
    public decimal CurrentStock { get; set; }
    public string? WarehouseName { get; set; }
    public string? RackName { get; set; }
    public DateTime? MfgDate { get; set; }
    public DateTime? ExpDate { get; set; }
    public string? BranchId { get; set; }
}
