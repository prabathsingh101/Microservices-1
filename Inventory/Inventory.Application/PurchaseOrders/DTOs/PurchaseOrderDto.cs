public class PurchaseOrderDto
{
    public int Id { get; set; }
    public string PoNumber { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; }
    public Guid PriceListId { get; set; }
    public DateTime PoDate { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal SubTotal { get; set; }
    public string Status { get; set; }
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public string? Remarks { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; } 

    // Child items list for Hierarchical Grid
    public List<PurchaseOrderItemDto> Items { get; set; } = new();

    // Manual Mapping from Entity to DTO
    public static PurchaseOrderDto FromEntity(dynamic entity)
    {
        var dto = new PurchaseOrderDto
        {
            Id = entity.Id,
            PoNumber = entity.PoNumber,
            SupplierId = entity.SupplierId,
            SupplierName = entity.SupplierName,
            PriceListId = entity.PriceListId,
            PoDate = entity.PoDate,
            TotalQuantity = entity.TotalQuantity,
            TotalTax = entity.TotalTax,
            SubTotal = entity.SubTotal,
            GrandTotal = entity.GrandTotal,
            Remarks = entity.Remarks,
            ExpectedDeliveryDate = entity.ExpectedDeliveryDate,
            Status = entity.Status,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy,
            CreatedDate = entity.CreatedDate,
            UpdatedDate = entity.UpdatedDate
        };

        // Agar child items exist karte hain toh unhe map karein
        if (entity.Items != null)
        {
            foreach (var item in entity.Items)
            {
                dto.Items.Add(PurchaseOrderItemDto.FromEntity(item));
            }
        }

        return dto;
    }
}