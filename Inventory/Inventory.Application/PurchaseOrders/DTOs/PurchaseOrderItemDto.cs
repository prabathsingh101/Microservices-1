public class PurchaseOrderItemDto
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Qty { get; set; }
    public string Unit { get; set; }
    public decimal Rate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal GstPercent { get; set; }

    public decimal ReceivedQty {  get; set; }
    public decimal PendingQty {  get; set; }
    public decimal AcceptedQty {  get; set; }
    public decimal RejectedQty {  get; set; }
    public decimal ReturnQty { get; set; }
    public decimal ReturnAmount { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsExpiryRequired { get; set; }
    public string? WarehouseName { get; set; }
    public string? RackName { get; set; }
    public bool IsAlreadyPurged { get; set; }

    // Manual Mapping from Entity to DTO
    public static PurchaseOrderItemDto FromEntity(dynamic entity)
    {
        var dto = new PurchaseOrderItemDto
        {
            Id = entity.Id,
            PurchaseOrderId = entity.PurchaseOrderId,
            ProductId = entity.ProductId,
            ProductName = entity.Product?.Name ?? "NA",
            Qty = entity.Qty,
            Unit = entity.Unit,
            Rate = entity.Rate,
            TaxAmount = entity.TaxAmount,
            Total = entity.Total,
            DiscountPercent = entity.DiscountPercent,
            GstPercent = entity.GstPercent,
            ReceivedQty = entity.ReceivedQty,
            ManufacturingDate = entity.MfgDate,
            ExpiryDate = entity.ExpDate,
            IsExpiryRequired = entity.Product != null ? entity.Product.IsExpiryRequired : false
        };

        return dto;
    }
}
