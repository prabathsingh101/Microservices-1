using MediatR;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using System;
using System.IO;

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

        if (!string.IsNullOrWhiteSpace(product.ImageUrl) && product.ImageUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            var savedPath = SaveBase64Image(product.ImageUrl, product.Id);
            if (!string.IsNullOrWhiteSpace(savedPath))
            {
                product.ImageUrl = savedPath;
            }
        }

        await _repository.AddAsync(product);

        await _context.SaveChangesAsync(cancellationToken);  

        return product.Id;
    }

    private string? SaveBase64Image(string? base64String, Guid productId)
    {
        if (string.IsNullOrWhiteSpace(base64String))
            return base64String;

        if (base64String.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var parts = base64String.Split(',');
                if (parts.Length != 2) return base64String;

                var metadata = parts[0];
                var base64Data = parts[1];

                var extension = ".png";
                if (metadata.Contains("jpeg") || metadata.Contains("jpg"))
                    extension = ".jpg";
                else if (metadata.Contains("webp"))
                    extension = ".webp";
                else if (metadata.Contains("gif"))
                    extension = ".gif";

                var bytes = Convert.FromBase64String(base64Data);

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"{productId}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                File.WriteAllBytes(filePath, bytes);

                return $"/uploads/products/{fileName}";
            }
            catch
            {
                return base64String;
            }
        }

        return base64String;
    }
}
