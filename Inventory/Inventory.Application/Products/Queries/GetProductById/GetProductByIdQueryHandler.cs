using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Application.Products.DTOs;
using MediatR;

namespace Inventory.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler
    : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _repository;

    public GetProductByIdQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductDto?> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var p = await _repository.GetByIdAsync(request.Id);
        if (p is null) return null;

        return new ProductDto
        {
            id = p.Id,
            categoryId = p.CategoryId,
            subcategoryId = p.SubcategoryId,
            productName = p.Name,
            sku = p.Sku,
            brand = p.Brand,
            unit = p.Unit,  
            hsnCode=p.HSNCode,
            basePurchasePrice = p.BasePurchasePrice,
            mrp = p.MRP,
            discount = p.Discount,
            saleRate = p.SaleRate,
            defaultGst = p.DefaultGst,
            minStock =p.MinStock,
            currentStock = p.CurrentStock,
            trackInventory = p.TrackInventory,
            isActive = p.IsActive,
            description = p.Description,
            createdBy = p.CreatedBy,
            damagedStock = p.DamagedStock,
            productType = int.TryParse(p.ProductType, out var type) ? type : 1,
            genericName = p.GenericName,
            manufacturer = p.Manufacturer,
            scheduleClass = p.ScheduleClass,
            defaultWarehouseId = p.DefaultWarehouseId,
            defaultRackId = p.DefaultRackId,
            isExpiryRequired = p.IsExpiryRequired,
            imageUrl = p.ImageUrl
        };
    }
}
