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
    public string? TaxType { get; set; }
    public decimal? TdsPercent { get; set; }
    public decimal? TdsAmount { get; set; }
    public decimal? TcsPercent { get; set; }
    public decimal? TcsAmount { get; set; }
    public decimal? IgstAmount { get; set; }
    public decimal? CgstAmount { get; set; }
    public decimal? SgstAmount { get; set; }
    public string Status { get; set; }
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool IsDispatched { get; set; }
    public decimal TotalOrdered { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalAccepted { get; set; }
    public decimal TotalRejected { get; set; }

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
            TaxType = entity.TaxType,
            TdsPercent = entity.TdsPercent,
            TdsAmount = entity.TdsAmount,
            TcsPercent = entity.TcsPercent,
            TcsAmount = entity.TcsAmount,
            IgstAmount = entity.IgstAmount,
            CgstAmount = entity.CgstAmount,
            SgstAmount = entity.SgstAmount,
            Remarks = entity.Remarks,
            ExpectedDeliveryDate = entity.ExpectedDeliveryDate,
            Status = entity.Status,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy,
            CreatedDate = entity.CreatedDate,
            UpdatedDate = entity.UpdatedDate,
            IsDispatched = entity.IsDispatched
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