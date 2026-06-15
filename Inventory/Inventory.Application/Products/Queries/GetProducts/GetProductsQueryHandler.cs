using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Application.Products.DTOs;
using MediatR;

namespace Inventory.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler
    : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    private readonly IProductRepository _repository;

    public GetProductsQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ProductDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _repository.GetAllAsync();

        return list.Select(p => new ProductDto
        {
            id = p.Id,
            categoryId = p.CategoryId,
            subcategoryId = p.SubcategoryId,
            sku = p.Sku,
            saleRate = p.SaleRate,
            brand = p.Brand,
            productName = p.Name,
            unit = p.Unit,   
            hsnCode =p.HSNCode,
            basePurchasePrice = p.BasePurchasePrice,
            mrp = p.MRP,
            discount = p.Discount,
            discountPercent = p.DiscountPercent,
            minStock =p.MinStock,
            currentStock = p.CurrentStock,
            defaultGst = p.DefaultGst,
            description = p.Description,
            trackInventory = p.TrackInventory,
            isActive = p.IsActive,
            createdBy = p.CreatedBy,
            createdOn = p.CreatedOn,
            modifiedBy = p.ModifiedBy,
            modifiedOn = p.ModifiedOn,
            genericName = p.GenericName,
            manufacturer = p.Manufacturer,
            scheduleClass = p.ScheduleClass,
            isExpiryRequired = p.IsExpiryRequired,
            imageUrl = p.ImageUrl
        }).ToList();
    }
}
