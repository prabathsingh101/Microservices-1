using Inventory.Application.Common.Interfaces;
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
            request.categoryid,
            request.subcategoryid,
            request.productname,
            request.sku, 
            request.saleRate,
            request.brand,
            request.unit,
            request.hsncode,
            request.basepurchaseprice,
            request.mrp,
            request.defaultgst,
            request.minstock,
            request.trackinventory,
            request.isactive,
            request.description,
            request.updatedby,
            request.productType,
            request.damagedStock,
            request.defaultwarehouseid,
            request.defaultrackid,
            request.isExpiryRequired
        );

        await _context.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
