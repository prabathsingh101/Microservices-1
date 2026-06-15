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
    private readonly IInventoryDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetProductsPagedQueryHandler(IProductRepository repository, 
        IInventoryDbContext context,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _context = context;
        _currentUserService = currentUserService;
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
                x.Subcategory.SubcategoryName.ToLower().Contains(search) ||
                (x.GenericName != null && x.GenericName.ToLower().Contains(search))
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
                    "categoryid" => Guid.TryParse(val, out var catId) ? query.Where(x => x.CategoryId == catId) : query,
                    "subcategoryid" => Guid.TryParse(val.Replace("\"", "").Trim(), out var subId) ? query.Where(x => x.SubcategoryId == subId) : query,
                    "categoryname" => query.Where(x => x.Category.CategoryName.ToLower().Contains(val)),
                    "subcategoryname" => query.Where(x => x.Subcategory.SubcategoryName.ToLower().Contains(val)),
                    "sku" => query.Where(x => x.Sku.ToLower().Contains(val)),
                    "hsncode" => query.Where(x => x.HSNCode.ToLower().Contains(val)),
                    "unit" => query.Where(x => x.Unit.ToLower().Contains(val)),
                    "pricelistid" => Guid.TryParse(val, out var plId) 
                        ? query.Where(x => _context.PriceListItems.Any(pi => pi.PriceListId == plId && pi.ProductId == x.Id)) 
                        : query,
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
            "currentstock" => query.OrderByDescending(x => x.CreatedOn),
            _ => query.OrderByDescending(x => x.CreatedOn)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var itemsData = await query
            .Include(p => p.Category)
            .Include(p => p.Subcategory)
            .Include(p => p.DefaultWarehouse)
            .Include(p => p.DefaultRack)
            .Skip((request.Request.PageNumber - 1) * request.Request.PageSize)
            .Take(request.Request.PageSize)
            .ToListAsync(cancellationToken);

        // Fetch Discount and Batch info in a more optimized way if needed, but for now 
        // let's just use the Master data to ensure binding works without timeout.
        // If we really need accurate per-batch info, we should do it in a single join/group query.

        // 🚀 SMART TRANSACTION-BASED STOCK CALCULATION
        var productIds = itemsData.Select(p => p.Id).ToList();
        var companyIdClaim = _currentUserService.CompanyId;
        var branchId = _currentUserService.BranchId;

        var stockLookup = await _context.WarehouseStocks
            .IgnoreQueryFilters()
            .Where(ws => ws.CompanyId == companyIdClaim && productIds.Contains(ws.ProductId))
            .Where(ws => string.IsNullOrEmpty(branchId) || ws.BranchId == branchId)
            .GroupBy(ws => ws.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty, cancellationToken);

        // 🚀 SMART BATCH DATE LOOKUP (For Earliest Batch)
        var batchLookup = await _context.GRNDetails
            .Where(g => productIds.Contains(g.ProductId) && g.GRNHeader.Status != "Cancelled")
            .GroupBy(g => g.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                MfgDate = g.OrderBy(x => x.ExpDate ?? DateTime.MaxValue).ThenBy(x => x.GRNHeader.ReceivedDate).Select(x => x.MfgDate).FirstOrDefault(),
                ExpDate = g.OrderBy(x => x.ExpDate ?? DateTime.MaxValue).ThenBy(x => x.GRNHeader.ReceivedDate).Select(x => x.ExpDate).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.ProductId, x => new { x.MfgDate, x.ExpDate }, cancellationToken);

        var items = itemsData.Select(p => {
            var actualStock = stockLookup.GetValueOrDefault(p.Id, 0);

            return new ProductDto
            {
                id = p.Id,
                categoryId = p.CategoryId,
                categoryName = p.Category.CategoryName,
                subcategoryId = p.SubcategoryId,
                subcategoryName = p.Subcategory.SubcategoryName,
                sku = p.Sku,
                mrp = p.MRP,
                saleRate = p.SaleRate,
                productName = p.Name,
                unit = p.Unit,
                hsnCode = p.HSNCode,
                minStock = p.MinStock,
                basePurchasePrice = p.BasePurchasePrice,
                currentStock = actualStock > 0 ? actualStock : 0, // ⚡ Accurate Live Stock
                damagedStock = p.DamagedStock,
                defaultGst = p.DefaultGst,
                discount = p.Discount,
                discountPercent = p.DiscountPercent,
                isExpiryRequired = p.IsExpiryRequired,
                productType = int.TryParse(p.ProductType, out var type) ? type : 1,
                description = p.Description,
                trackInventory = p.TrackInventory,
                defaultWarehouseId = p.DefaultWarehouseId,
                defaultWarehouseName = p.DefaultWarehouse != null ? p.DefaultWarehouse.Name : null,
                defaultRackId = p.DefaultRackId,
                defaultRackName = p.DefaultRack != null ? p.DefaultRack.Name : null,
                imageUrl = p.ImageUrl,
                createdOn = p.CreatedOn,
                modifiedOn = p.ModifiedOn,
                genericName = p.GenericName,
                manufacturer = p.Manufacturer,
                scheduleClass = p.ScheduleClass,
                manufacturingDate = batchLookup.TryGetValue(p.Id, out var batch) ? batch.MfgDate : null,
                expiryDate = batchLookup.TryGetValue(p.Id, out var b) ? b.ExpDate : null
            };
        }).ToList();

        return new GridResponse<ProductDto>(items, totalCount);
    }
}
