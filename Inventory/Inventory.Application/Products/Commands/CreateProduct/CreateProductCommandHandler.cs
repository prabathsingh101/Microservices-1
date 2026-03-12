using MediatR;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;

namespace Inventory.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler
    : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _repository;
    private readonly IInventoryDbContext _context;
    public CreateProductCommandHandler(IProductRepository repository, 
        IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<Guid> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = new Product(
            request.categoryid,
            request.subcategoryid,
            request.productname,
            request.sku,          
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
            request.createdby,
            request.saleRate,
            request.productType,
            request.damagedStock,
            request.defaultwarehouseid,
            request.defaultrackid,
            request.isExpiryRequired
        )
        {

        };
        await _repository.AddAsync(product);

        await _context.SaveChangesAsync(cancellationToken);  

        return product.Id;
    }
}
