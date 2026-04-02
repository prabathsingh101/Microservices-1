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

    public ProductRepository(InventoryDbContext db)
    {
        _db = db;
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
        return await _db.Products
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _db.Products
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Product>> GetByCategoryIdAsync(Guid categoryId)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(x => x.CategoryId == categoryId)
            .ToListAsync();
    }

    public async Task<List<Product>> GetBySubcategoryIdAsync(Guid subcategoryId)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(x => x.SubcategoryId == subcategoryId)
            .ToListAsync();
    }

    public IQueryable<Product> Query()
    {
        return _db.Products.AsQueryable();
    }
    public void DeleteRange(List<Product> products)
    {
        _db.Products.RemoveRange(products);
    }

    public async Task<List<Product>> GetByIdsAsync(List<Guid> ids)
    {
        return await _db.Products
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();
    }

    public async Task<bool> HasPriceListAsync(List<Guid> ProductsIds)
    {
        return await _db.Products
           .AnyAsync(x => ProductsIds.Contains(x.Id));
    }

    public async Task<List<Product>> SearchActiveProductsAsync(string term)
    {
        return await _db.Products
         .AsNoTracking() // Read-only query optimization
         .Where(p => p.IsActive && p.Name.Contains(term)) // Sirf Name par search lagayein
         .Take(20)
         .ToListAsync();
    }
    public async Task<ProductRateDto> GetProductRateAsync(Guid productId, Guid? priceListId)
    {
        // 1. Pehle hum dhoondhenge ki kaunsa rate aur discount apply karna hai
        decimal finalRate = 0;
        decimal finalDiscount = 0;

        var priceQuery = _db.PriceListItems.AsNoTracking()
            .Where(pli => pli.ProductId == productId);

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
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.CurrentStock <= p.MinStock) // Dashboard wala logic
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
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.CurrentStock <= p.MinStock)
            .Select(p => new ExcelExportDto
            {
                ProductName = p.Name,
                SKU = p.Sku,
                Category = p.Category.CategoryName,
                CurrentStock = p.CurrentStock,
                MinStock = p.MinStock,
                Unit = p.Unit
            })
            .ToListAsync();
    }
    public async Task<List<StockMovementDto>> GetRecentMovementsPagedAsync(int pageNumber, int pageSize)
    {
        // 1. Purchase Orders se movements nikalna
        var purchases = _db.PurchaseOrders
            .AsNoTracking()
            .Select(po => new StockMovementDto
            {
                Product = "PO: " + po.PoNumber, // Ya item name join karke
                Type = "Purchase",
                Qty = po.GrandTotal, // Simplified for example
                Date = po.CreatedDate,
                Status = po.Status
            });

        // 2. Sale Orders se movements nikalna
        var sales = _db.SaleOrders
            .AsNoTracking()
            .Select(so => new StockMovementDto
            {
                Product = "SO: " + so.Id,
                Type = "Sale",
                Qty = so.GrandTotal,
                Date = so.CreatedAt,
                Status = "Completed"
            });

        // 3. Combine, Sort aur Paginate karna (Virtual Scroll support)
        return await purchases.Union(sales)
            .OrderByDescending(x => x.Date)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
    public async Task<(int successCount, List<string> errors)> UploadProductsAsync(IFormFile file)
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
                    "DamagedStock", "ProductType", "TrackInventory", "Active", "Description" 
                };
                
                var actualHeaders = new List<string>();
                for (int i = 1; i <= 18; i++)
                {
                    actualHeaders.Add(headerRow.Cell(i).Value.ToString()?.Trim());
                }

                if (!expectedHeaders.SequenceEqual(actualHeaders))
                {
                    errors.Add($"Invalid Template: Headers mismatch. Expected: {string.Join(", ", expectedHeaders)}");
                    return (0, errors);
                }

                var dataRows = rows.Skip(1);

                // 2. Pre-fetch Categories and Subcategories for faster lookup
                var categories = await _db.Categories.AsNoTracking().ToDictionaryAsync(c => c.CategoryName.ToLower().Trim(), c => c.Id);
                var subcats = await _db.Subcategories.AsNoTracking().Select(s => new { s.Id, s.SubcategoryName, s.CategoryId }).ToListAsync();
                
                // 3. Pre-fetch existing products for duplicate check
                var products = await _db.Products.AsNoTracking().Select(p => new { p.Name, p.Sku }).ToListAsync();
                var dbNameSet = new HashSet<string>(products.Select(p => p.Name.ToLower().Trim()));
                var dbSkuSet = new HashSet<string>(products.Where(p => !string.IsNullOrEmpty(p.Sku)).Select(p => p.Sku.ToLower().Trim()));

                // In-file duplicate tracking
                var fileNames = new HashSet<string>();
                var fileSkus = new HashSet<string>();

                var newProducts = new List<Product>();

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
                        var active = row.Cell(17).Value.ToString()?.Trim().ToUpper() == "TRUE";
                        var desc = row.Cell(18).Value.ToString()?.Trim();

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

                        // Subcategory Lookup (must belong to the category)
                        var subInfo = subcats.FirstOrDefault(s => s.SubcategoryName.ToLower().Trim() == subName.ToLower() && s.CategoryId == catId);
                        if (subInfo == null)
                        {
                            errors.Add($"Row {rowNum}: Subcategory '{subName}' not found or doesn't belong to Category '{catName}'.");
                            continue;
                        }

                        // Duplicate Check
                        if (fileNames.Contains(name.ToLower())) { errors.Add($"Row {rowNum}: Duplicate Product Name '{name}' in file."); continue; }
                        if (dbNameSet.Contains(name.ToLower())) { errors.Add($"Row {rowNum}: Product Name '{name}' already exists in DB."); continue; }
                        
                        if (!string.IsNullOrEmpty(sku))
                        {
                            if (fileSkus.Contains(sku.ToLower())) { errors.Add($"Row {rowNum}: Duplicate SKU '{sku}' in file."); continue; }
                            if (dbSkuSet.Contains(sku.ToLower())) { errors.Add($"Row {rowNum}: SKU '{sku}' already exists in DB."); continue; }
                            fileSkus.Add(sku.ToLower());
                        }
                        fileNames.Add(name.ToLower());

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
                            damagedStock
                        );

                        newProducts.Add(product);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {rowNum}: Fatal Error - {ex.Message}");
                    }
                }

                if (newProducts.Any())
                {
                    await _db.Products.AddRangeAsync(newProducts);
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

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
    {
        var query = _db.Products.AsNoTracking()
            .Where(p => p.Name.ToLower().Trim() == name.ToLower().Trim());

        if (excludeId.HasValue && excludeId != Guid.Empty)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}
