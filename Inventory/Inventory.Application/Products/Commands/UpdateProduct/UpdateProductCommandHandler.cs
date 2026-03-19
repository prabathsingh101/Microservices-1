using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using MediatR;

namespace Inventory.Application.Products.Commands.UpdateProduct;

internal sealed class UpdateProductCommandHandler
    : IRequestHandler<UpdateProductCommand, Guid>
{
    private readonly IProductRepository _repository;
    private readonly IInventoryDbContext _context;

    public UpdateProductCommandHandler(
        IProductRepository repository,
        IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<Guid> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id);

        if (product is null)
            throw new KeyNotFoundException("Product not found");

        product.Update(
            categoryid: request.categoryid,
            subcategoryid: request.subcategoryid,
            name: request.productname,
            sku: request.sku,
            saleRate: request.saleRate,
            brand: request.brand,
            unit: request.unit,
            hsncode: request.hsncode,
            basepurchaseprice: request.basepurchaseprice,
            mrp: request.mrp,
            defaultGst: request.defaultgst,
            minstock: request.minstock,
            trackinventory: request.trackinventory,
            isactive: request.isactive,
            description: request.description,
            updatedby: request.updatedby,
            productType: request.productType,
            damagedStock: request.damagedStock,
            defaultWarehouseId: request.defaultwarehouseid,
            defaultRackId: request.defaultrackid,
            isExpiryRequired: request.isExpiryRequired,
            imageUrl: request.imageUrl
        );

        await _context.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
