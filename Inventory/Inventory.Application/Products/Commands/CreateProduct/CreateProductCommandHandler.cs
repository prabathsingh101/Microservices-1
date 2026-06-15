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
            categoryid: request.CategoryId,
            subcategoryid: request.SubcategoryId,
            productname: request.ProductName,
            sku: request.Sku,
            brand: request.Brand,
            unit: request.Unit,
            hsncode: request.HsnCode,
            basepurchaseprice: request.BasePurchasePrice,
            mrp: request.Mrp,
            discount: request.Discount,
            discountPercent: request.DiscountPercent,
            defaultgst: request.DefaultGst,
            minstock: request.MinStock,
            trackinventory: request.TrackInventory,
            isactive: request.IsActive,
            description: request.Description,
            createdby: request.CreatedBy,
            saleRate: request.SaleRate,
            productType: request.ProductType,
            damagedStock: request.DamagedStock,
            genericName: request.GenericName,
            manufacturer: request.Manufacturer,
            scheduleClass: request.ScheduleClass,
            defaultWarehouseId: request.DefaultWarehouseId,
            defaultRackId: request.DefaultRackId,
            isExpiryRequired: request.IsExpiryRequired,
            imageUrl: request.ImageUrl,
            companyId: request.CompanyId,
            branchId: request.BranchId
        );
        await _repository.AddAsync(product);

        await _context.SaveChangesAsync(cancellationToken);  

        return product.Id;
    }
}
