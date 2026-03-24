public class CreateSaleReturnDto
{
    public DateTime ReturnDate { get; set; }
    public int SaleOrderId { get; set; }
    public int CustomerId { get; set; }
    public string? Remarks { get; set; }
    public bool IsQuick { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    public List<SaleReturnItemDto> Items { get; set; } = new();
}

public class SaleReturnItemDto
{
    public Guid ProductId { get; set; } //
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
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}