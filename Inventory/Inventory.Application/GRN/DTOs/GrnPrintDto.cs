public class GrnPrintDto
{
    public int Id { get; set; }
    public string GrnNumber { get; set; } = string.Empty;
    public int? PurchaseOrderId { get; set; }
    public string? PoNumber { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty; // Microservice se resolve hoga
    public DateTime ReceivedDate { get; set; }
    public string Status { get; set; } = string.Empty; // e.g., "Received"
    public string? GatePassNo { get; set; }
    public string Remarks { get; set; } = string.Empty;

    // Footer Calculations
    public decimal SubTotal { get; set; } // items ka total without tax
    public decimal TotalTaxAmount { get; set; } // pura GST amount
    public decimal TotalAmount { get; set; } // Final Amount (SubTotal + Tax)

    public List<GrnItemPrintDto> Items { get; set; } = new();
}

public class GrnItemPrintDto
{
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty; // e.g., "PCS"

    // Quantities
    public decimal OrderedQty { get; set; }
    public decimal PendingQty { get; set; }
    public decimal AcceptedQty { get; set; }
    public decimal RejectedQty { get; set; }
    public decimal ReceivedQty { get; set; }

    // Pricing, Discount & Tax
    public decimal UnitRate { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal GstPercentage { get; set; } // Product master se
    public decimal GstAmount { get; set; } // (ReceivedQty * UnitRate) * (GstPercentage / 100)

    // Line Total (Qty * Rate)
    public decimal Total { get; set; } // Yeh Tax include karke ya exclude karke aapki marzi, standard exclude hota hai
}