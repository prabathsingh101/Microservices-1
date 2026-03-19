using MediatR;

namespace Inventory.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    Guid categoryid,
    Guid subcategoryid,
    string productname,
    string sku,
    string brand,
    string unit,
    string hsncode,
    decimal basepurchaseprice,
    decimal mrp,
    decimal defaultgst,
    int minstock,
    bool trackinventory,
    bool isactive,
    string? description,
    string updatedby,
    decimal saleRate,
    string productType,
    decimal damagedStock,
    Guid? defaultwarehouseid,
    Guid? defaultrackid,
    bool isExpiryRequired,
    string? imageUrl
) : IRequest<Guid>;
