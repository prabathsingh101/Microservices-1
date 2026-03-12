using MediatR;

public sealed record CreateProductCommand(
    Guid categoryid,
    Guid subcategoryid,    
    string productname,
    string sku,
    string brand,
    string unit,
    string hsncode,   
    decimal basepurchaseprice,
    decimal mrp,
    decimal saleRate,
    decimal defaultgst,
    int minstock,
    bool trackinventory,
    bool isactive,
    string? description,
    string createdby,
    string productType,
    decimal damagedStock,
    Guid? defaultwarehouseid,
    Guid? defaultrackid,
    bool isExpiryRequired
) : IRequest<Guid>;
