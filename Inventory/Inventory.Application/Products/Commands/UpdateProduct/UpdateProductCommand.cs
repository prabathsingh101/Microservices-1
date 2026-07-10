using MediatR;
using System;
using System.Collections.Generic;

namespace Inventory.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    Guid CategoryId,
    Guid SubcategoryId,
    string ProductName,
    string Sku,
    string Brand,
    string Unit,
    string HsnCode,
    decimal BasePurchasePrice,
    decimal Mrp,
    decimal Discount,
    decimal DiscountPercent,
    decimal DefaultGst,
    int MinStock,
    bool TrackInventory,
    bool IsActive,
    string? Description,
    string? ModifiedBy,
    decimal SaleRate,
    string ProductType,
    decimal DamagedStock,
    string? GenericName,
    string? Manufacturer,
    string? ScheduleClass,
    Guid? DefaultWarehouseId,
    Guid? DefaultRackId,
    bool IsExpiryRequired,
    string? ImageUrl,
    Guid CompanyId,
    string? BranchId = null,
    string? Gender = null,
    string? FabricType = null,
    string? FitStyle = null,
    string? SizeGroup = null,
    List<ProductVariantDto>? Variants = null
) : IRequest<Guid>;
