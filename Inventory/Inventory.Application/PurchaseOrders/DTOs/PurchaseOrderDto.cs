public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string PoNumber { get; set; }
    public Guid SupplierId { get; set; }
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
    public string ModifiedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public bool IsDispatched { get; set; }
    public decimal TotalOrdered { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalAccepted { get; set; }
    public decimal TotalRejected { get; set; }
    public decimal TotalReturned { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount => GrandTotal - PaidAmount;
    public string PaymentStatus => DueAmount <= 0 ? "Paid" : (PaidAmount > 0 ? "Partial" : "Unpaid");

    public string? Remarks { get; set; }
    public string? GrnNumber { get; set; }
    public Guid? GrnId { get; set; }

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
            ModifiedBy = entity.ModifiedBy,
            CreatedOn = entity.CreatedOn,
            ModifiedOn = entity.ModifiedOn,
            IsDispatched = entity.IsDispatched
        };

        // Mapping item list
        if (entity.Items != null)
        {
            foreach (var item in entity.Items)
            {
                dto.Items.Add(PurchaseOrderItemDto.FromEntity(item));
            }
        }

        // Aggregate Totals calculation
        if (dto.Items != null && dto.Items.Any())
        {
            dto.TotalOrdered = dto.Items.Sum(x => x.Qty);
        }

        if (entity.GrnHeaders != null)
        {
            var allGrnHeaders = (IEnumerable<dynamic>)entity.GrnHeaders;
            var firstGrn = allGrnHeaders.FirstOrDefault();
            if (firstGrn != null)
            {
                dto.GrnNumber = firstGrn.GRNNumber;
            }

            var allGrnDetails = allGrnHeaders
                .SelectMany(h => (IEnumerable<dynamic>)h.GRNItems ?? Enumerable.Empty<dynamic>())
                .ToList();

            dto.TotalReceived = allGrnDetails.Sum(x => (decimal)x.ReceivedQty);
            dto.TotalAccepted = allGrnDetails.Sum(x => (decimal)x.AcceptedQty);
            dto.TotalRejected = allGrnDetails.Sum(x => (decimal)x.RejectedQty);

            // Per-item Distribution
            foreach (var item in dto.Items)
            {
                var itemGrns = allGrnDetails.Where(gd => gd.ProductId == item.ProductId).ToList();
                if (itemGrns.Any())
                {
                    item.ReceivedQty = itemGrns.Sum(x => (decimal)x.ReceivedQty);
                    item.AcceptedQty = itemGrns.Sum(x => (decimal)x.AcceptedQty);
                    item.RejectedQty = itemGrns.Sum(x => (decimal)x.RejectedQty);
                }
            }
        }

        return dto;
    }
}
