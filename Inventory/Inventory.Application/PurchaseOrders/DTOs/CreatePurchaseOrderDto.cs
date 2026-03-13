public record CreatePurchaseOrderDto(
    int SupplierId,
    string SupplierName,
    Guid PriceListId,
    DateTime PoDate,
    DateTime? ExpectedDeliveryDate,
    string Remarks,
    decimal TotalTax,
    decimal SubTotal,
    decimal GrandTotal,
    string CreatedBy,
    List<PoItemDto> Items);

public record PoItemDto(
    Guid ProductId,
    decimal Qty,
    string Unit,
    decimal Rate,
    decimal DiscountPercent,
    decimal GstPercent,
    decimal TaxAmount,
    decimal Total,
    DateTime? ManufacturingDate,
    DateTime? ExpiryDate);