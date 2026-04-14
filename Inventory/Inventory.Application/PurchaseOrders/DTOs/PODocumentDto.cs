public class PODocumentDto
{
    // PurchaseOrders Table Se
    public string PoNumber { get; set; }
    public string SupplierName { get; set; }
    public DateTime PoDate { get; set; }
    public string Remarks { get; set; }
    public decimal TotalTax { get; set; }
    public decimal SubTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Status { get; set; }
    public string CreatedBy { get; set; }

    // PurchaseOrderItems Table Se (List of products)
    public List<POItemDocumentDto> Items { get; set; }
}

public class POItemDocumentDto
{
    // PurchaseOrderItems Table Se

    public Guid ProductId { get; set; }
    public string ProductName { get; set; } // Isse Product table se join karke layenge
    public decimal Qty { get; set; }
    public string Unit { get; set; }
    public decimal Rate { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal GstPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public DateTime? MfgDate { get; set; }
    public DateTime? ExpDate { get; set; }
}
