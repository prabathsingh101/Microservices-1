using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetProductSearchHandler : IRequestHandler<GetProductSearchQuery, List<ProductSearchResponseDto>>
{
    private readonly IProductRepository _repository;
    private readonly IInventoryDbContext _context;

    public GetProductSearchHandler(IProductRepository repository, IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<List<ProductSearchResponseDto>> Handle(GetProductSearchQuery request, CancellationToken cancellationToken)
    {
        // 1. Optimized Search using AsNoTracking to improve performance
        // Hum base query ko AsNoTracking ke saath handle kar rahe hain
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.DefaultRack) // Include Rack for naming
            .Where(p => p.IsActive &&
                        (p.Name.Contains(request.Term) || p.Sku.Contains(request.Term)))
            .ToListAsync(cancellationToken);

        var productDtos = new List<ProductSearchResponseDto>();

        foreach (var p in products)
        {
            // 🆕 STEP 2: Fetch Discount from PriceListItems
            // Ye PriceList se discount uthayega, aur agar nahi hai toh 0 rakhega
            var discountPercent = await _context.PriceListItems
                .AsNoTracking()
                .Where(di => di.ProductId == p.Id)
                .Select(di => di.DiscountPercent)
                .FirstOrDefaultAsync(cancellationToken);

            productDtos.Add(new ProductSearchResponseDto
            {
                id = p.Id,
                name = p.Name,
                isActive = p.IsActive,
                basePurchasePrice = p.BasePurchasePrice,
                unit = p.Unit,
                brand = p.Brand,
                sku = p.Sku,
                hsncode = p.HSNCode,
                mrp = p.MRP,
                saleRate = p.SaleRate ?? 0,

                // 🆕 GST Product Master se aur Discount PriceList se
                gstPercent = p.DefaultGst ?? 0,
                discountPercent = discountPercent,

                // STEP 3: DIRECT BINDING WITH DATABASE COLUMN
                currentStock = (decimal)p.CurrentStock,
                defaultRackName = p.DefaultRack != null ? p.DefaultRack.Name : null,

                // 🆕 Expiry Requirement & Earliest Batch Dates
                isExpiryRequired = p.IsExpiryRequired,
                manufacturingDate = await _context.GRNDetails
                    .AsNoTracking()
                    .Where(g => g.ProductId == p.Id && (g.ReceivedQty - g.RejectedQty) > 0)
                    .OrderBy(g => g.ExpDate ?? DateTime.MaxValue)
                    .Select(g => g.MfgDate)
                    .FirstOrDefaultAsync(cancellationToken),
                expiryDate = await _context.GRNDetails
                    .AsNoTracking()
                    .Where(g => g.ProductId == p.Id && (g.ReceivedQty - g.RejectedQty) > 0)
                    .OrderBy(g => g.ExpDate ?? DateTime.MaxValue)
                    .Select(g => g.ExpDate)
                    .FirstOrDefaultAsync(cancellationToken)
            });
        }

        return productDtos;
    }
}