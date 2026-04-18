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
            categoryid: request.categoryid,
            subcategoryid: request.subcategoryid,
            productname: request.productname,
            sku: request.sku,
            brand: request.brand,
            unit: request.unit,
            hsncode: request.hsncode,
            basepurchaseprice: request.basepurchaseprice,
            mrp: request.mrp,
            discount: request.discount,
            defaultgst: request.defaultgst,
            minstock: request.minstock,
            trackinventory: request.trackinventory,
            isactive: request.isactive,
            description: request.description,
            createdby: request.createdby,
            saleRate: request.saleRate,
            productType: request.productType,
            damagedStock: request.damagedStock,
            defaultWarehouseId: request.defaultwarehouseid,
            defaultRackId: request.defaultrackid,
            isExpiryRequired: request.isExpiryRequired,
            imageUrl: request.imageUrl,
            companyId: request.CompanyId
        );
        await _repository.AddAsync(product);

        await _context.SaveChangesAsync(cancellationToken);  

        return product.Id;
    }
}
