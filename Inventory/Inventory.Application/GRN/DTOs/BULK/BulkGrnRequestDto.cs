using Inventory.Application.GRN.DTOs.BULK;

public class BulkGrnRequestDto
{
    public List<Guid> PurchaseOrderIds { get; set; } = new List<Guid>();
    public string CreatedBy { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public DateTime ReceivedDate { get; set; } = DateTime.Now;
    public string? GatePassNo { get; set; }
    public string? Remarks { get; set; }
    public List<BulkItemRequestDto> Items { get; set; } = new List<BulkItemRequestDto>();
}
