using System;
using System.Collections.Generic;

public record UpdateRfqDto(
    Guid Id,
    Guid SupplierId,
    string? SupplierName,
    DateTime? ExpiryDate,
    string? Remarks,
    string ModifiedBy,
    List<UpdateRfqItemDto> Items
);

public record UpdateRfqItemDto(
    Guid? Id, // Null for new items
    Guid ProductId,
    decimal Qty,
    decimal? UnitPrice,
    decimal? TaxRate,
    decimal? Discount,
    decimal? TotalCost
);
