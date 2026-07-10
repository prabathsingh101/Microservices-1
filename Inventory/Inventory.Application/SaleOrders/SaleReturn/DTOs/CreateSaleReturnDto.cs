public class CreateSaleReturnDto
{
    public DateTime ReturnDate { get; set; }
    public Guid SaleOrderId { get; set; }
    public Guid CustomerId { get; set; }
    public string? Remarks { get; set; }
    public bool IsQuick { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    public Guid? CompanyId { get; set; }
    public string? BranchId { get; set; }
    public string ReturnMode { get; set; } = "RefundOnly";
    public List<SaleReturnItemDto> Items { get; set; } = new();
    public List<SaleExchangeItemDto> ExchangeItems { get; set; } = new();
}

public class SaleReturnItemDto
{
    public Guid? CompanyId { get; set; }
    public string? BranchId { get; set; }
    public Guid ProductId { get; set; } //
    public Guid? ProductVariantId { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public decimal ReturnQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxPercentage { get; set; }
    public string? Reason { get; set; }
    public string? ItemCondition { get; set; }
    public DateTime? MfgDate { get; set; }
    public DateTime? ExpDate { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? RackId { get; set; }
    public string? BatchNumber { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

public class SaleExchangeItemDto
{
    public Guid? CompanyId { get; set; }
    public string? BranchId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxPercentage { get; set; }
    public DateTime? MfgDate { get; set; }
    public DateTime? ExpDate { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? RackId { get; set; }
    public string? BatchNumber { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}
