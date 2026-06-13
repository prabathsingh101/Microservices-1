using System;
using System.Collections.Generic;

public record CreateRfqDto(
    Guid SupplierId,
    string? SupplierName,
    DateTime? ExpiryDate,
    string? Remarks,
    Guid CompanyId,
    string? BranchId,
    string CreatedBy,
    List<CreateRfqItemDto> Items,
    bool IsQuick = false
);

public record CreateRfqItemDto(
    Guid ProductId,
    decimal Qty,
    decimal? UnitPrice,
    decimal? TaxRate,
    decimal? Discount,
    decimal? TotalCost
);
