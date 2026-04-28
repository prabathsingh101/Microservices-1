public class SaleReturnListDto
{
    public Guid SaleReturnHeaderId { get; set; } // PK from DB
    public string ReturnNumber { get; set; } = string.Empty; //
    public DateTime ReturnDate { get; set; } //
    public Guid CustomerId { get; set; } // To fetch name from Microservice
    public string CustomerName { get; set; } = string.Empty; // Will be mapped in Service
    public string ProductName { get; set; } = string.Empty; // Added for UI scannability
    public decimal TotalQty { get; set; } // Added to prevent multiple UI detail calls
    public string SoRef { get; set; } = string.Empty; // SONumber from SaleOrders
    public decimal TotalAmount { get; set; } //
    public string Status { get; set; } = string.Empty; //
    public string? GatePassNo { get; set; }
    public bool IsQuick { get; set; }
}

public class SaleReturnPagedResponse
{
    public List<SaleReturnListDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
