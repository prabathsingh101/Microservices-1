using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Application.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Inventory.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler
    : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _repository;
    private readonly IInventoryDbContext _context;

    public GetProductByIdQueryHandler(IProductRepository repository, IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<ProductDto?> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var p = await _repository.GetByIdAsync(request.Id);
        if (p is null) return null;

        var variantsList = await _context.ProductVariants
            .Where(v => v.ProductId == p.Id)
            .Select(v => new ProductVariantDto(
                v.Size,
                v.Color,
                v.Barcode,
                v.SKU,
                v.AdditionalPrice,
                v.CurrentStock,
                v.IsActive
            ))
            .ToListAsync(cancellationToken);

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
            discountPercent = p.DiscountPercent,
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
            imageUrl = p.ImageUrl,
            gender = p.Gender,
            fabricType = p.FabricType,
            fitStyle = p.FitStyle,
            sizeGroup = p.SizeGroup,
            variants = variantsList
        };
    }
}
