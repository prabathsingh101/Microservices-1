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
        var branchId = _currentUserService.BranchId;

        var products = await _db.Products
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .ToListAsync();

        // 🚀 SMART STOCK CALCULATION: Recalculate based on context (Branch or Global)
        var productIds = products.Select(p => p.Id).ToList();
        var stockQuery = _db.WarehouseStocks
            .IgnoreQueryFilters() // 🚀 Bypass branch restriction for global totals
            .Where(ws => ws.CompanyId == companyId && productIds.Contains(ws.ProductId));

        if (!string.IsNullOrEmpty(branchId))
        {
            stockQuery = stockQuery.Where(ws => ws.BranchId == branchId);
        }

        var stockLookup = await stockQuery
            .GroupBy(ws => ws.ProductId)
            .Select(g => new { ProductId = g.Key, TotalQty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.TotalQty);

        foreach (var p in products)
        {
            p.CurrentStock = stockLookup.GetValueOrDefault(p.Id, 0);
        }

        return products;
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
        
        return _db.Products.Where(x => x.CompanyId == companyId);
    }

    public void DeleteRange(List<Product> products)
    {
        _db.Products.RemoveRange(products);
    }

    public async Task<List<Product>> GetByIdsAsync(List<Guid> ids)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        return await _db.Products
            .Where(x => ids.Contains(x.Id) && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId || x.BranchId == null))
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
        var branchId = _currentUserService.BranchId;

        var products = await _db.Products
         .AsNoTracking()
         .Where(p => p.CompanyId == companyId && p.IsActive && p.Name.Contains(term))
         .Take(20)
         .ToListAsync();

        // 🚀 SMART TRANSACTION-BASED STOCK CALCULATION
        var productIds = products.Select(p => p.Id).ToList();

        var receivedStock = await _db.GRNDetails.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(gd => gd.CompanyId == companyId && productIds.Contains(gd.ProductId))
            .Where(gd => string.IsNullOrEmpty(branchId) || gd.BranchId == branchId)
            .GroupBy(gd => gd.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.ReceivedQty - x.RejectedQty) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty);

        var soldStock = await _db.SaleOrderItems.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(soi => soi.CompanyId == companyId && productIds.Contains(soi.ProductId) && (soi.SaleOrder.Status == "Confirmed" || soi.SaleOrder.Status == "Delivered"))
            .Where(soi => string.IsNullOrEmpty(branchId) || soi.BranchId == branchId)
            .GroupBy(soi => soi.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => (decimal?)x.Qty) ?? 0 })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty);

        var purchaseReturnedStock = await _db.PurchaseReturnItems.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(pri => pri.CompanyId == companyId && productIds.Contains(pri.ProductId))
            .Where(pri => string.IsNullOrEmpty(branchId) || pri.BranchId == branchId)
            .GroupBy(pri => pri.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => (decimal?)x.ReturnQty) ?? 0 })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty);

        var saleReturnedStock = await _db.SaleReturnItems.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(sri => sri.CompanyId == companyId && productIds.Contains(sri.ProductId))
            .Where(sri => string.IsNullOrEmpty(branchId) || sri.BranchId == branchId)
            .GroupBy(sri => sri.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => (decimal?)x.ReturnQty) ?? 0 })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty);

        foreach (var p in products)
        {
            var received = receivedStock.GetValueOrDefault(p.Id, 0);
            var sold = soldStock.GetValueOrDefault(p.Id, 0);
            var purReturned = purchaseReturnedStock.GetValueOrDefault(p.Id, 0);
            var saleReturned = saleReturnedStock.GetValueOrDefault(p.Id, 0);
            p.CurrentStock = received - sold - purReturned + saleReturned;
        }

        return products;
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
        var branchId = _currentUserService.BranchId;

        var query = _db.Products
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId && p.IsActive);

        var products = await query.Select(p => new LowStockProductDto
        {
            Id = p.Id,
            CategoryName = p.Category.CategoryName,
            SubCategoryName = p.Subcategory.SubcategoryName,
            ProductName = p.Name,
            SKU = p.Sku,
            Unit = p.Unit,
            BasePurchasePrice = p.BasePurchasePrice,
            MinStock = p.MinStock,
            CurrentStock = _db.WarehouseStocks
                .Where(ws => ws.ProductId == p.Id && (string.IsNullOrEmpty(branchId) || ws.BranchId == branchId))
                .Sum(ws => (decimal?)ws.Quantity) ?? 0
        }).ToListAsync();

        // Filter by low stock condition after getting the branch/global quantity
        return products.Where(p => p.CurrentStock <= p.MinStock).ToList();
    }

    public async Task<List<ExcelExportDto>> GetLowStockExportDataAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;

        var products = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.DefaultWarehouse)
            .Include(p => p.DefaultRack)
            .Where(p => p.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || p.BranchId == branchId || p.BranchId == null))
            .Select(p => new ExcelExportDto
            {
                ProductName = p.Name,
                SKU = p.Sku,
                Category = p.Category.CategoryName,
                CurrentStock = _db.WarehouseStocks
                    .Where(ws => ws.ProductId == p.Id && (string.IsNullOrEmpty(branchId) || ws.BranchId == branchId))
                    .Sum(ws => (decimal?)ws.Quantity) ?? 0,
                MinStock = p.MinStock,
                Discount = p.Discount,
                Unit = p.Unit,
                Warehouse = p.DefaultWarehouse != null ? p.DefaultWarehouse.Name : "-",
                Rack = p.DefaultRack != null ? p.DefaultRack.Name : "-",
                IsExpiryRequired = p.IsExpiryRequired
            })
            .ToListAsync();

        return products.Where(p => p.CurrentStock <= p.MinStock).ToList();
    }
    public async Task<List<StockMovementDto>> GetRecentMovementsPagedAsync(int pageNumber, int pageSize)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        // 1. Purchase Orders se movements nikalna
        var purchases = _db.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
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
            .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
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
    public async Task<(int successCount, int updateCount, List<string> errors)> UploadProductsAsync(IFormFile file, Guid companyId, string? branchId = null)
    {
        var errors = new List<string>();
        int successCount = 0;
        int updateCount = 0;

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
                    return (0, 0, errors);
                }

                var expectedHeaders = new List<string> { 
                    "Category", "Subcategory", "ProductName", "SKU", "Brand", "Unit", 
                    "BasePrice", "MRP", "Discount", "SaleRate", "GST%", "HSNCode", "MinStock", 
                    "DamagedStock", "ProductType", "TrackInventory", "RequiresExpiry", "Active", 
                    "DefaultWarehouse", "DefaultRack", "Description" 
                };
                
                var actualHeaders = new List<string>();
                for (int i = 1; i <= expectedHeaders.Count; i++)
                {
                    var val = headerRow.Cell(i).GetValue<string>().Replace("\"", "").Trim();
                    if (!string.IsNullOrEmpty(val)) actualHeaders.Add(val);
                }

                bool headersMatch = expectedHeaders.All(eh =>
                    actualHeaders.Any(ah => string.Equals(ah, eh, StringComparison.OrdinalIgnoreCase)));

                if (!headersMatch)
                {
                    errors.Add($"Invalid Template: Headers mismatch. Expected: {string.Join(", ", expectedHeaders)}");
                    return (0, 0, errors);
                }

                var dataRows = rows.Skip(1);

                // 2. Pre-fetch dependencies for faster lookup
                var categoriesList = await _db.Categories.Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId)).AsNoTracking().ToListAsync();
                var categories = categoriesList.GroupBy(c => (c.CategoryName ?? "").ToLower().Trim()).ToDictionary(g => g.Key, g => g.First().Id);
                
                var subcats = await _db.Subcategories.Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId)).AsNoTracking().Select(s => new { s.Id, s.SubcategoryName, s.CategoryId }).ToListAsync();
                
                var warehousesList = await _db.Warehouses.Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId)).AsNoTracking().ToListAsync();
                var warehouses = warehousesList.GroupBy(w => (w.Name ?? "").ToLower().Trim()).ToDictionary(g => g.Key, g => g.First().Id);
                
                var racks = await _db.Racks.Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId)).AsNoTracking().Select(r => new { r.Id, r.Name, r.WarehouseId }).ToListAsync();
                
                // 3. Pre-fetch existing products for Upsert logic
                var dbProducts = await _db.Products.Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId || x.BranchId == null)).ToListAsync();
                var dbProductsByName = dbProducts.GroupBy(p => (p.Name ?? "").ToLower().Trim()).ToDictionary(g => g.Key, g => g.First());
                
                // For SKU lookup
                var dbProductsBySku = new Dictionary<string, Product>();
                foreach(var p in dbProducts.Where(p => !string.IsNullOrEmpty(p.Sku)))
                {
                    var skuKey = (p.Sku ?? "").ToLower().Trim();
                    if (!dbProductsBySku.ContainsKey(skuKey)) dbProductsBySku.Add(skuKey, p);
                }

                // In-file duplicate tracking
                var fileNames = new HashSet<string>();
                var fileSkus = new HashSet<string>();

                // 4. PROCESS ROWS
                foreach (var row in dataRows)
                {
                    int rowNum = row.RowNumber();
                    try
                    {
                        var catName = row.Cell(1).GetValue<string>()?.Trim();
                        var subName = row.Cell(2).GetValue<string>()?.Trim();
                        var name = row.Cell(3).GetValue<string>()?.Trim();
                        var sku = row.Cell(4).GetValue<string>()?.Trim();
                        var brand = row.Cell(5).GetValue<string>()?.Trim();
                        var unit = row.Cell(6).GetValue<string>()?.Trim();
                        var basePriceVal = row.Cell(7).Value;
                        var mrpVal = row.Cell(8).Value;
                        var discountVal = row.Cell(9).Value;
                        var saleRateVal = row.Cell(10).Value;
                        var gstVal = row.Cell(11).Value;
                        var hsn = row.Cell(12).GetValue<string>()?.Trim();
                        var minStockVal = row.Cell(13).Value;
                        var damagedStockVal = row.Cell(14).Value;
                        var pType = row.Cell(15).GetValue<string>()?.Trim();
                        
                        var trackInvRaw = row.Cell(16).GetValue<string>()?.Trim().ToLower();
                        var trackInv = trackInvRaw == "true" || trackInvRaw == "yes" || trackInvRaw == "1";
                        
                        var reqExpiryRaw = row.Cell(17).GetValue<string>()?.Trim().ToLower();
                        var reqExpiry = reqExpiryRaw == "true" || reqExpiryRaw == "yes" || reqExpiryRaw == "1";
                        
                        var activeRaw = row.Cell(18).GetValue<string>()?.Trim().ToLower();
                        var active = string.IsNullOrWhiteSpace(activeRaw) || activeRaw == "true" || activeRaw == "yes" || activeRaw == "1" || activeRaw == "active";

                        var whName = row.Cell(19).GetValue<string>()?.Trim();
                        var rackName = row.Cell(20).GetValue<string>()?.Trim();
                        var desc = row.Cell(21).GetValue<string>()?.Trim();

                        // Skip Empty Rows
                        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(catName)) continue;

                        // Validation
                        if (string.IsNullOrWhiteSpace(name)) { errors.Add($"Row {rowNum}: ProductName is required."); continue; }
                        if (string.IsNullOrWhiteSpace(catName)) { errors.Add($"Row {rowNum}: Category is required."); continue; }
                        if (string.IsNullOrWhiteSpace(subName)) { errors.Add($"Row {rowNum}: Subcategory is required."); continue; }
                        if (string.IsNullOrWhiteSpace(unit)) { errors.Add($"Row {rowNum}: Unit is required."); continue; }

                        // Category Lookup
                        if (!categories.TryGetValue((catName ?? "").ToLower().Trim(), out var catId))
                        {
                            var dbCat = await _db.Categories.FirstOrDefaultAsync(x => x.CompanyId == companyId && ((x.CategoryName ?? "").ToLower().Trim() == (catName ?? "").ToLower().Trim() || (x.CategoryCode ?? "").ToLower().Trim() == (catName ?? "").ToLower().Trim()));
                            if (dbCat != null) catId = dbCat.Id;
                            else { errors.Add($"Row {rowNum}: Category '{catName}' not found."); continue; }
                        }

                        // Subcategory Lookup
                        var subInfo = subcats.FirstOrDefault(s => (s.SubcategoryName ?? "").ToLower().Trim() == (subName ?? "").ToLower().Trim() && s.CategoryId == catId);
                        if (subInfo == null)
                        {
                            var dbSub = await _db.Subcategories.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.CategoryId == catId && ((x.SubcategoryName ?? "").ToLower().Trim() == (subName ?? "").ToLower().Trim() || (x.SubcategoryCode ?? "").ToLower().Trim() == (subName ?? "").ToLower().Trim()));
                            if (dbSub != null) subInfo = new { dbSub.Id, dbSub.SubcategoryName, dbSub.CategoryId };
                            else { errors.Add($"Row {rowNum}: Subcategory '{subName}' not found."); continue; }
                        }

                        // Warehouse Lookup
                        Guid? warehouseId = null;
                        if (!string.IsNullOrWhiteSpace(whName))
                        {
                            if (warehouses.TryGetValue(whName?.ToLower().Trim() ?? "", out var wId)) warehouseId = wId;
                            else errors.Add($"Row {rowNum}: Warning - Warehouse '{whName}' not found.");
                        }

                        // Rack Lookup (if warehouse found)
                        Guid? rackId = null;
                        if (!string.IsNullOrWhiteSpace(rackName) && warehouseId.HasValue)
                        {
                            var rInfo = racks.FirstOrDefault(r => (r.Name ?? "").ToLower().Trim() == rackName?.ToLower().Trim() && r.WarehouseId == warehouseId);
                            if (rInfo != null) rackId = rInfo.Id;
                            else errors.Add($"Row {rowNum}: Warning - Rack '{rackName}' not found in Warehouse '{whName}'.");
                        }

                        // In-file duplicate checking
                        if (fileNames.Contains(name?.ToLower().Trim() ?? "")) { errors.Add($"Row {rowNum}: Duplicate Product Name '{name}' in file."); continue; }
                        if (!string.IsNullOrEmpty(sku) && fileSkus.Contains(sku?.ToLower().Trim() ?? "")) { errors.Add($"Row {rowNum}: Duplicate SKU '{sku}' in file."); continue; }
                        
                        fileNames.Add(name?.ToLower().Trim() ?? "");
                        if (!string.IsNullOrEmpty(sku)) fileSkus.Add(sku?.ToLower().Trim() ?? "");

                        // Parsing
                        decimal basePrice = 0, mrp = 0, discount = 0, saleRate = 0, gst = 0, damagedStock = 0;
                        int minStock = 0;

                        if (!basePriceVal.IsBlank) decimal.TryParse(basePriceVal.ToString(), out basePrice);
                        if (!mrpVal.IsBlank) decimal.TryParse(mrpVal.ToString(), out mrp);
                        if (!discountVal.IsBlank) decimal.TryParse(discountVal.ToString(), out discount);
                        if (!saleRateVal.IsBlank) decimal.TryParse(saleRateVal.ToString(), out saleRate);
                        if (!gstVal.IsBlank)
                        {
                            var gstStr = gstVal.ToString().Replace("%", "").Trim();
                            decimal.TryParse(gstStr, out gst);
                        }
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
                        
                        if (!string.IsNullOrEmpty(sku) && dbProductsBySku.TryGetValue(sku?.ToLower().Trim() ?? "", out var pBySku))
                        {
                            existingProduct = pBySku;
                        }
                        else if (dbProductsByName.TryGetValue(name?.ToLower().Trim() ?? "", out var pByName))
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
                                companyId: companyId,
                                branchId: branchId
                            );
                            updateCount++;
                        }
                        else
                        {
                            var product = new Product(
                                categoryid: catId,
                                subcategoryid: subInfo.Id,
                                productname: name,
                                sku: sku ?? "",
                                brand: brand ?? "",
                                unit: unit,
                                hsncode: hsn ?? "",
                                basepurchaseprice: basePrice,
                                mrp: mrp,
                                discount: discount,
                                defaultgst: gst,
                                minstock: minStock,
                                trackinventory: trackInv,
                                isactive: active,
                                description: desc ?? "",
                                createdby: "BulkUpload",
                                saleRate: saleRate,
                                productType: mappedType,
                                damagedStock: damagedStock,
                                defaultWarehouseId: warehouseId,
                                defaultRackId: rackId,
                                isExpiryRequired: reqExpiry,
                                imageUrl: null,
                                companyId: companyId,
                                branchId: branchId
                            );
                            await _db.Products.AddAsync(product);
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {rowNum}: Fatal Error - {ex.Message}");
                    }
                }

                if (successCount > 0 || updateCount > 0)
                {
                    await _db.SaveChangesAsync();
                }

                if (successCount == 0 && !errors.Any())
                {
                    errors.Add("No valid rows found in the file.");
                }
            }
        }
        return (successCount, updateCount, errors);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid companyId, Guid? excludeId = null)
    {
        var branchId = _currentUserService.BranchId;
        var query = _db.Products.AsNoTracking()
            .Where(p => p.CompanyId == companyId && p.Name.ToLower().Trim() == name.ToLower().Trim() && (string.IsNullOrEmpty(branchId) || p.BranchId == branchId || p.BranchId == null));

        if (excludeId.HasValue && excludeId != Guid.Empty)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}
