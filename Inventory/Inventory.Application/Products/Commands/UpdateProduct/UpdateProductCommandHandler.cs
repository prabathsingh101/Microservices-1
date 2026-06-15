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
            categoryid: request.CategoryId,
            subcategoryid: request.SubcategoryId,
            name: request.ProductName,
            sku: request.Sku,
            saleRate: request.SaleRate,
            discount: request.Discount,
            discountPercent: request.DiscountPercent,
            brand: request.Brand,
            unit: request.Unit,
            hsncode: request.HsnCode,
            basepurchaseprice: request.BasePurchasePrice,
            mrp: request.Mrp,
            defaultGst: request.DefaultGst,
            minstock: request.MinStock,
            trackinventory: request.TrackInventory,
            isactive: request.IsActive,
            description: request.Description,
            ModifiedBy: request.ModifiedBy,
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

        await _context.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
