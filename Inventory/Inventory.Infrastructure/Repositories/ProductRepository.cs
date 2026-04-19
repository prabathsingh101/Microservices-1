using ClosedXML.Excel;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.DTOs;
using Inventory.Application.Stock;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly InventoryDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public ProductRepository(InventoryDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task AddAsync(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _db.Products.Update(product);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Product product)
    {
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _db.Products
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId);
    }

    public async Task<List<Product>> GetAllAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _db.Products
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .ToListAsync();
    }

    public async Task<List<Product>> GetByCategoryIdAsync(Guid categoryId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _db.Products
            .AsNoTracking()
            .Where(x => x.CategoryId == categoryId && x.CompanyId == companyId)
            .ToListAsync();
    }

    public async Task<List<Product>> GetBySubcategoryIdAsync(Guid subcategoryId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _db.Products
            .AsNoTracking()
            .Where(x => x.SubcategoryId == subcategoryId && x.CompanyId == companyId)
            .ToListAsync();
    }

    public IQueryable<Product> Query()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return _db.Products.Where(x => x.CompanyId == companyId).AsQueryable();
    }
    public void DeleteRange(List<Product> products)
    {
        _db.Products.RemoveRange(products);
    }

    public async Task<List<Product>> GetByIdsAsync(List<Guid> ids)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _db.Products
            .Where(x => ids.Contains(x.Id) && x.CompanyId == companyId)
            .ToListAsync();
    }

    public async Task<bool> HasPriceListAsync(List<Guid> ProductsIds)
    {
        return await _db.Products
           .AnyAsync(x => ProductsIds.Contains(x.Id));
    }

    public async Task<List<Product>> SearchActiveProductsAsync(string term)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _db.Products
         .AsNoTracking()
         .Where(p => p.CompanyId == companyId && p.IsActive && p.Name.Contains(term))
         .Take(20)
         .ToListAsync();
    }
    public async Task<ProductRateDto> GetProductRateAsync(Guid productId, Guid? priceListId)
    {
        // 1. Pehle hum dhoondhenge ki kaunsa rate aur discount apply karna hai
        decimal finalRate = 0;
        decimal finalDiscount = 0;

        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var priceQuery = _db.PriceListItems.AsNoTracking()
            .Where(pli => pli.ProductId == productId && pli.CompanyId == companyId);

        if (priceListId.HasValue && priceListId != Guid.Empty)
        {
            var pli = await priceQuery
                .Where(pli => pli.PriceListId == priceListId)
                .Select(pli => new { pli.Rate, pli.DiscountPercent })
                .FirstOrDefaultAsync();
            
            if (pli != null)
            {
                finalRate = pli.Rate;
                finalDiscount = pli.DiscountPercent;
            }
        }
        else
        {
            // AUTOMATIC LOGIC: Latest Active Purchase PriceList dhoondho
            var pli = await priceQuery
                .Where(pli => pli.PriceList.IsActive == true &&
                              pli.PriceList.PriceType == "PURCHASE" &&
                              pli.PriceList.ValidFrom <= DateTime.Now &&
                              pli.PriceList.ValidTo >= DateTime.Now)
                .OrderByDescending(pli => pli.PriceList.CreatedOn)
                .Select(pli => new { pli.Rate, pli.DiscountPercent })
                .FirstOrDefaultAsync();

            if (pli != null)
            {
                finalRate = pli.Rate;
                finalDiscount = pli.DiscountPercent;
            }
        }

        // 2. Product Master details ke saath data bind karein
        var productDetails = await _db.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new ProductRateDto(
                p.Id,                                     // 1. ProductId
                priceListId,                              // 2. PriceListId
                finalRate,                                // 3. PriceListRate
                p.BasePurchasePrice,                      // 4. BasePurchasePrice
                p.Unit ?? "PCS",                          // 5. Unit
                p.DefaultGst ?? 0m,                       // 6. GstPercent
                p.HSNCode ?? "",                          // 7. HsnCode
                finalDiscount                             // 8. DiscountPercent
            ))
            .FirstOrDefaultAsync();

        if (productDetails == null)
        {
            throw new Exception("Product not found in Master.");
        }

        return productDetails;
    }

    public async Task<IEnumerable<LowStockProductDto>> GetLowStockProductsAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId && p.IsActive && p.CurrentStock <= p.MinStock) // Dashboard wala logic
            .Select(p => new LowStockProductDto
            {
                Id = p.Id,
                CategoryName = p.Category.CategoryName, // Join logic
                SubCategoryName = p.Subcategory.SubcategoryName,
                ProductName = p.Name,
                SKU = p.Sku,
                Unit = p.Unit,
                CurrentStock = p.CurrentStock,
                MinStock = p.MinStock,
                BasePurchasePrice = p.BasePurchasePrice
            })
            .ToListAsync();
    }

    public async Task<List<ExcelExportDto>> GetLowStockExportDataAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var products = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.DefaultWarehouse)
            .Include(p => p.DefaultRack)
            .Where(p => p.CompanyId == companyId && p.CurrentStock <= p.MinStock)
            .Select(p => new ExcelExportDto
            {
                ProductName = p.Name,
                SKU = p.Sku,
                Category = p.Category.CategoryName,
                CurrentStock = p.CurrentStock,
                MinStock = p.MinStock,
                Discount = p.Discount,
                Unit = p.Unit,
                Warehouse = p.DefaultWarehouse != null ? p.DefaultWarehouse.Name : "-",
                Rack = p.DefaultRack != null ? p.DefaultRack.Name : "-",
                IsExpiryRequired = p.IsExpiryRequired
            })
            .ToListAsync();
        return products;
    }
    public async Task<List<StockMovementDto>> GetRecentMovementsPagedAsync(int pageNumber, int pageSize)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        // 1. Purchase Orders se movements nikalna
        var purchases = _db.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .Select(po => new StockMovementDto
            {
                Product = "PO: " + po.PoNumber, 
                Type = "Purchase",
                Qty = po.GrandTotal, 
                Date = po.CreatedOn,
                Status = po.Status
            });

        // 2. Sale Orders se movements nikalna
        var sales = _db.SaleOrders
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .Select(so => new StockMovementDto
            {
                Product = "SO: " + so.Id,
                Type = "Sale",
                Qty = so.GrandTotal,
                Date = so.CreatedOn,
                Status = "Completed"
            });

        // 3. Combine, Sort aur Paginate karna (Virtual Scroll support)
        return await purchases.Union(sales)
            .OrderByDescending(x => x.Date)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
    public async Task<(int successCount, List<string> errors)> UploadProductsAsync(IFormFile file, Guid companyId)
    {
        var errors = new List<string>();
        int successCount = 0;

        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            using (var workbook = new XLWorkbook(stream))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed();

                // 1. Header Validation
                var headerRow = rows.FirstOrDefault();
                if (headerRow == null)
                {
                    errors.Add("Invalid Template: File is empty.");
                    return (0, errors);
                }

                var expectedHeaders = new List<string> { 
                    "Category", "Subcategory", "ProductName", "SKU", "Brand", "Unit", 
                    "BasePrice", "MRP", "Discount", "SaleRate", "GST%", "HSNCode", "MinStock", 
                    "DamagedStock", "ProductType", "TrackInventory", "RequiresExpiry", "Active", 
                    "DefaultWarehouse", "DefaultRack", "Description" 
                };
                
                var actualHeaders = new List<string>();
                for (int i = 1; i <= 21; i++)
                {
                    actualHeaders.Add(headerRow.Cell(i).Value.ToString()?.Trim());
                }

                if (!expectedHeaders.SequenceEqual(actualHeaders))
                {
                    errors.Add($"Invalid Template: Headers mismatch. Expected: {string.Join(", ", expectedHeaders)}");
                    return (0, errors);
                }

                var dataRows = rows.Skip(1);

                // 2. Pre-fetch dependencies for faster lookup
                var categories = await _db.Categories.Where(x => x.CompanyId == companyId).AsNoTracking().ToDictionaryAsync(c => c.CategoryName.ToLower().Trim(), c => c.Id);
                var subcats = await _db.Subcategories.Where(x => x.CompanyId == companyId).AsNoTracking().Select(s => new { s.Id, s.SubcategoryName, s.CategoryId }).ToListAsync();
                var warehouses = await _db.Warehouses.Where(x => x.CompanyId == companyId).AsNoTracking().ToDictionaryAsync(w => w.Name.ToLower().Trim(), w => w.Id);
                var racks = await _db.Racks.Where(x => x.CompanyId == companyId).AsNoTracking().Select(r => new { r.Id, r.Name, r.WarehouseId }).ToListAsync();
                
                // 3. Pre-fetch existing products for Upsert logic
                var dbProducts = await _db.Products.Where(x => x.CompanyId == companyId).ToListAsync();
                var dbProductsByName = dbProducts.ToDictionary(p => p.Name.ToLower().Trim(), p => p);
                
                // For SKU lookup
                var dbProductsBySku = new Dictionary<string, Product>();
                foreach(var p in dbProducts.Where(p => !string.IsNullOrEmpty(p.Sku)))
                {
                    var skuKey = p.Sku.ToLower().Trim();
                    if (!dbProductsBySku.ContainsKey(skuKey)) dbProductsBySku.Add(skuKey, p);
                }

                // In-file duplicate tracking
                var fileNames = new HashSet<string>();
                var fileSkus = new HashSet<string>();

                var newProducts = new List<Product>();
                int updateCount = 0;

                foreach (var row in dataRows)
                {
                    int rowNum = row.RowNumber();
                    try
                    {
                        var catName = row.Cell(1).Value.ToString()?.Trim();
                        var subName = row.Cell(2).Value.ToString()?.Trim();
                        var name = row.Cell(3).Value.ToString()?.Trim();
                        var sku = row.Cell(4).Value.ToString()?.Trim();
                        var brand = row.Cell(5).Value.ToString()?.Trim();
                        var unit = row.Cell(6).Value.ToString()?.Trim();
                        var basePriceVal = row.Cell(7).Value;
                        var mrpVal = row.Cell(8).Value;
                        var discountVal = row.Cell(9).Value;
                        var saleRateVal = row.Cell(10).Value;
                        var gstVal = row.Cell(11).Value;
                        var hsn = row.Cell(12).Value.ToString()?.Trim();
                        var minStockVal = row.Cell(13).Value;
                        var damagedStockVal = row.Cell(14).Value;
                        var pType = row.Cell(15).Value.ToString()?.Trim();
                        var trackInv = row.Cell(16).Value.ToString()?.Trim().ToUpper() == "TRUE";
                        var reqExpiry = row.Cell(17).Value.ToString()?.Trim().ToUpper() == "TRUE";
                        var active = row.Cell(18).Value.ToString()?.Trim().ToUpper() == "TRUE";
                        var whName = row.Cell(19).Value.ToString()?.Trim();
                        var rackName = row.Cell(20).Value.ToString()?.Trim();
                        var desc = row.Cell(21).Value.ToString()?.Trim();

                        // Skip Empty Rows
                        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(catName)) continue;

                        // Validation
                        if (string.IsNullOrWhiteSpace(name)) { errors.Add($"Row {rowNum}: ProductName is required."); continue; }
                        if (string.IsNullOrWhiteSpace(catName)) { errors.Add($"Row {rowNum}: Category is required."); continue; }
                        if (string.IsNullOrWhiteSpace(subName)) { errors.Add($"Row {rowNum}: Subcategory is required."); continue; }
                        if (string.IsNullOrWhiteSpace(unit)) { errors.Add($"Row {rowNum}: Unit is required."); continue; }

                        // Category Lookup
                        if (!categories.TryGetValue(catName.ToLower(), out var catId))
                        {
                            errors.Add($"Row {rowNum}: Category '{catName}' not found.");
                            continue;
                        }

                        // Subcategory Lookup
                        var subInfo = subcats.FirstOrDefault(s => s.SubcategoryName.ToLower().Trim() == subName.ToLower() && s.CategoryId == catId);
                        if (subInfo == null)
                        {
                            errors.Add($"Row {rowNum}: Subcategory '{subName}' not found or doesn't belong to Category '{catName}'.");
                            continue;
                        }

                        // Warehouse Lookup
                        Guid? warehouseId = null;
                        if (!string.IsNullOrWhiteSpace(whName))
                        {
                            if (warehouses.TryGetValue(whName.ToLower(), out var wId)) warehouseId = wId;
                            else errors.Add($"Row {rowNum}: Warning - Warehouse '{whName}' not found.");
                        }

                        // Rack Lookup (if warehouse found)
                        Guid? rackId = null;
                        if (!string.IsNullOrWhiteSpace(rackName) && warehouseId.HasValue)
                        {
                            var rInfo = racks.FirstOrDefault(r => r.Name.ToLower().Trim() == rackName.ToLower() && r.WarehouseId == warehouseId);
                            if (rInfo != null) rackId = rInfo.Id;
                            else errors.Add($"Row {rowNum}: Warning - Rack '{rackName}' not found in Warehouse '{whName}'.");
                        }

                        // In-file duplicate checking
                        if (fileNames.Contains(name.ToLower())) { errors.Add($"Row {rowNum}: Duplicate Product Name '{name}' in file."); continue; }
                        if (!string.IsNullOrEmpty(sku) && fileSkus.Contains(sku.ToLower())) { errors.Add($"Row {rowNum}: Duplicate SKU '{sku}' in file."); continue; }
                        
                        fileNames.Add(name.ToLower());
                        if (!string.IsNullOrEmpty(sku)) fileSkus.Add(sku.ToLower());

                        // Parsing
                        decimal basePrice = 0, mrp = 0, discount = 0, saleRate = 0, gst = 0, damagedStock = 0;
                        int minStock = 0;

                        if (!basePriceVal.IsBlank) decimal.TryParse(basePriceVal.ToString(), out basePrice);
                        if (!mrpVal.IsBlank) decimal.TryParse(mrpVal.ToString(), out mrp);
                        if (!discountVal.IsBlank) decimal.TryParse(discountVal.ToString(), out discount);
                        if (!saleRateVal.IsBlank) decimal.TryParse(saleRateVal.ToString(), out saleRate);
                        if (!gstVal.IsBlank) decimal.TryParse(gstVal.ToString(), out gst);
                        if (!damagedStockVal.IsBlank) decimal.TryParse(damagedStockVal.ToString(), out damagedStock);
                        if (!minStockVal.IsBlank) int.TryParse(minStockVal.ToString(), out minStock);

                        // Product Type Mapping
                        string mappedType = pType?.ToLower() switch
                        {
                            "finished" => "1",
                            "goods" => "2",
                            "raw material" => "3",
                            _ => "1"
                        };

                        // 4. UPSERT LOGIC
                        Product? existingProduct = null;
                        
                        if (!string.IsNullOrEmpty(sku) && dbProductsBySku.TryGetValue(sku.ToLower().Trim(), out var pBySku))
                        {
                            existingProduct = pBySku;
                        }
                        else if (dbProductsByName.TryGetValue(name.ToLower().Trim(), out var pByName))
                        {
                            existingProduct = pByName;
                        }

                        if (existingProduct != null)
                        {
                            existingProduct.Update(
                                categoryid: catId,
                                subcategoryid: subInfo.Id,
                                name: name,
                                sku: sku ?? "",
                                saleRate: saleRate,
                                discount: discount,
                                brand: brand ?? "",
                                unit: unit,
                                hsncode: hsn ?? "",
                                basepurchaseprice: basePrice,
                                mrp: mrp,
                                defaultGst: gst,
                                minstock: minStock,
                                trackinventory: trackInv,
                                isactive: active,
                                description: desc,
                                ModifiedBy: "BulkUpload",
                                productType: mappedType,
                                damagedStock: damagedStock,
                                defaultWarehouseId: warehouseId,
                                defaultRackId: rackId,
                                isExpiryRequired: reqExpiry,
                                modifiedon: DateTime.UtcNow,
                                companyId: companyId
                            );
                            updateCount++;
                        }
                        else
                        {
                            var product = new Product(
                                catId,
                                subInfo.Id,
                                name,
                                sku ?? "",
                                brand ?? "",
                                unit,
                                hsn ?? "",
                                basePrice,
                                mrp,
                                discount,
                                gst,
                                minStock,
                                trackInv,
                                active,
                                desc,
                                "BulkUpload",
                                saleRate,
                                mappedType,
                                damagedStock,
                                warehouseId,
                                rackId,
                                reqExpiry,
                                null, // ImageUrl
                                companyId
                            );
                            newProducts.Add(product);
                        }
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {rowNum}: Fatal Error - {ex.Message}");
                    }
                }

                if (newProducts.Any() || updateCount > 0)
                {
                    if (newProducts.Any()) await _db.Products.AddRangeAsync(newProducts);
                    await _db.SaveChangesAsync();
                }
                else if (!errors.Any())
                {
                    errors.Add("No valid rows found in the file.");
                }
            }
        }
        return (successCount, errors);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid companyId, Guid? excludeId = null)
    {
        var query = _db.Products.AsNoTracking()
            .Where(p => p.CompanyId == companyId && p.Name.ToLower().Trim() == name.ToLower().Trim());

        if (excludeId.HasValue && excludeId != Guid.Empty)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}
