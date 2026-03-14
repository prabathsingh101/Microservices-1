using ClosedXML;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Common.Models;
using Inventory.Application.Products.Queries.GetProducts;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

internal sealed class GetProductsPagedQueryHandler
    : IRequestHandler<GetProductsPagedQuery, GridResponse<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly IInventoryDbContext _context; // 🆕 Context added to fetch Discounts

    public GetProductsPagedQueryHandler(IProductRepository repository, 
        IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<GridResponse<ProductDto>> Handle(
            GetProductsPagedQuery request,
            CancellationToken cancellationToken)
    {
        var query = _repository.Query();

        // 🔍 SEARCH (Global) - Existing logic preserved
        if (!string.IsNullOrWhiteSpace(request.Request.Search))
        {
            var search = request.Request.Search.ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                x.HSNCode.ToLower().Contains(search) ||
                x.Sku.ToLower().Contains(search) ||
                x.Category.CategoryName.ToLower().Contains(search) ||
                x.Subcategory.SubcategoryName.ToLower().Contains(search)
            );
        }

        // 🔍 FILTERS (Column Specific) - Existing logic preserved
        if (request.Request.Filters != null && request.Request.Filters.Any())
        {
            foreach (var filter in request.Request.Filters)
            {
                if (string.IsNullOrWhiteSpace(filter.Value)) continue;

                var val = filter.Value.ToLower().Trim();
                query = filter.Key.ToLower() switch
                {
                    "productname" or "name" => query.Where(x => x.Name.ToLower().Contains(val)),
                    "categoryname" => query.Where(x => x.Category.CategoryName.ToLower().Contains(val)),
                    "subcategoryname" => query.Where(x => x.Subcategory.SubcategoryName.ToLower().Contains(val)),
                    "sku" => query.Where(x => x.Sku.ToLower().Contains(val)),
                    "hsncode" => query.Where(x => x.HSNCode.ToLower().Contains(val)),
                    "unit" => query.Where(x => x.Unit.ToLower().Contains(val)),
                    _ => query
                };
            }
        }

        // 🔃 SORT - Existing logic preserved
        query = request.Request.SortBy?.ToLower() switch
        {
            "productname" or "name" => request.Request.SortDirection == "asc"
                ? query.OrderBy(x => x.Name)
                : query.OrderByDescending(x => x.Name),
            "hsncode" => request.Request.SortDirection == "asc"
                ? query.OrderBy(x => x.HSNCode)
                : query.OrderByDescending(x => x.HSNCode),
            "sku" => request.Request.SortDirection == "asc"
                ? query.OrderBy(x => x.Sku)
                : query.OrderByDescending(x => x.Sku),
            "categoryname" => request.Request.SortDirection == "asc"
                ? query.OrderBy(x => x.Category.CategoryName)
                : query.OrderByDescending(x => x.Category.CategoryName),
            "subcategoryname" => request.Request.SortDirection == "asc"
                ? query.OrderBy(x => x.Subcategory.SubcategoryName)
                : query.OrderByDescending(x => x.Subcategory.SubcategoryName),
            "unit" => request.Request.SortDirection == "asc"
                ? query.OrderBy(x => x.Unit)
                : query.OrderByDescending(x => x.Unit),
            "minstock" => request.Request.SortDirection == "asc"
                ? query.OrderBy(x => x.MinStock)
                : query.OrderByDescending(x => x.MinStock),
            "currentstock" => request.Request.SortDirection == "asc"
                ? query.OrderBy(x => x.CurrentStock)
                : query.OrderByDescending(x => x.CurrentStock),
            _ => query.OrderByDescending(x => x.CreatedOn)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var itemsData = await query
            .Skip((request.Request.PageNumber - 1) * request.Request.PageSize)
            .Take(request.Request.PageSize)
            .Select(p => new
            {
                p.Id,
                p.CategoryId,
                CategoryName = p.Category.CategoryName,
                p.SubcategoryId,
                SubcategoryName = p.Subcategory.SubcategoryName,
                p.Sku,
                p.SaleRate,
                p.Name,
                p.Unit,
                p.HSNCode,
                p.MinStock,
                p.BasePurchasePrice,
                p.CurrentStock,
                p.DamagedStock,
                p.DefaultGst,
                p.IsExpiryRequired,
                p.Description,
                p.CreatedBy,
                p.CreatedOn,
                p.ModifiedBy,
                p.ModifiedOn,
                p.TrackInventory,
                p.ProductType,
                p.DefaultWarehouseId,
                DefaultWarehouseName = p.DefaultWarehouse != null ? p.DefaultWarehouse.Name : null,
                p.DefaultRackId,
                DefaultRackName = p.DefaultRack != null ? p.DefaultRack.Name : null,

                // 🆕 Fetch Discount from PriceListItems (Global logic)
                DiscountPercent = _context.PriceListItems
                    .Where(li => li.ProductId == p.Id)
                    .Select(li => li.DiscountPercent)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var items = itemsData.Select(p => new ProductDto
        {
            id = p.Id,
            categoryId = p.CategoryId,
            categoryName = p.CategoryName,
            subcategoryId = p.SubcategoryId,
            subcategoryName = p.SubcategoryName,
            sku = p.Sku,
            saleRate = p.SaleRate,
            productName = p.Name,
            unit = p.Unit,
            hsnCode = p.HSNCode,
            minStock = p.MinStock,
            basePurchasePrice = p.BasePurchasePrice,
            currentStock = p.CurrentStock,
            damagedStock = p.DamagedStock,
            defaultGst = p.DefaultGst,
            isExpiryRequired = p.IsExpiryRequired,

            // 🆕 Mapping New Field
            discountPercent = p.DiscountPercent,

            productType = int.TryParse(p.ProductType, out var type) ? type : 1,
            description = p.Description,
            createdBy = p.CreatedBy,
            createdOn = p.CreatedOn,
            modifiedBy = p.ModifiedBy,
            modifiedOn = p.ModifiedOn,
            trackInventory = p.TrackInventory,
            defaultWarehouseId = p.DefaultWarehouseId,
            defaultWarehouseName = p.DefaultWarehouseName,
            defaultRackId = p.DefaultRackId,
            defaultRackName = p.DefaultRackName
        }).ToList();

        // 🆕 Fetch Mfg and Exp dates for each item based on available stock
        foreach (var item in items)
        {
            var earliestBatch = await _context.GRNDetails
                .AsNoTracking()
                .Where(g => g.ProductId == item.id && (g.ReceivedQty - g.RejectedQty) > 0)
                .OrderBy(g => g.ExpDate ?? DateTime.MaxValue)
                .Select(g => new { g.MfgDate, g.ExpDate })
                .FirstOrDefaultAsync(cancellationToken);

            if (earliestBatch != null)
            {
                item.manufacturingDate = earliestBatch.MfgDate;
                item.expiryDate = earliestBatch.ExpDate;
            }
        }

        return new GridResponse<ProductDto>(items, totalCount);
    }
}