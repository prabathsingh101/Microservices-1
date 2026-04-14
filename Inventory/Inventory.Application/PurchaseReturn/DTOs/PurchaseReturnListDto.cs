
public class PurchaseReturnListDto
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty; // PR-20260204...
    public DateTime ReturnDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string GrnRef { get; set; } = string.Empty; // Multiple references ho toh comma-separated
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Completed";
    public string? GatePassNo { get; set; }
    public bool IsQuick { get; set; }
}

public class PurchaseReturnPagedResponse
{
    public List<PurchaseReturnListDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
