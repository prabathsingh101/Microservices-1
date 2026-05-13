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

        var stockLookup = await _db.WarehouseStocks
            .IgnoreQueryFilters()
            .Where(ws => ws.CompanyId == companyId && productIds.Contains(ws.ProductId))
            .Where(ws => string.IsNullOrEmpty(branchId) || ws.BranchId == branchId)
            .GroupBy(ws => ws.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty);

        foreach (var p in products)
        {
            p.CurrentStock = stockLookup.GetValueOrDefault(p.Id, 0);
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

        try
        {
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0; // 🚀 Reset stream position

                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1);
                    if (worksheet == null)
                    {
                        errors.Add("Invalid Template: No worksheet found.");
                        return (0, 0, errors);
                    }

                    var rows = worksheet.RangeUsed()?.RowsUsed();
                    if (rows == null)
                    {
                        errors.Add("Invalid Template: File is empty or has no data.");
                        return (0, 0, errors);
                    }

                    // 1. Header Validation
                    var headerRow = rows.FirstOrDefault();
                    if (headerRow == null)
                    {
                        errors.Add("Invalid Template: Header row missing.");
                        return (0, 0, errors);
                    }

                    var expectedHeaders = new List<string> { 
                        "Category", "Subcategory", "ProductName", "SKU", "Brand", "Unit", 
                        "BasePrice", "MRP", "Discount", "SaleRate", "GST%", "HSNCode", "MinStock", 
                        "DamagedStock", "ProductType", "TrackInventory", "RequiresExpiry", "Active", 
                        "DefaultWarehouse", "DefaultRack", "Description" 
                    };
                    
                    var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? expectedHeaders.Count + 10;
                    
                    for (int i = 1; i <= lastCol; i++)
                    {
                        var cellVal = headerRow.Cell(i).GetValue<string>()?.Replace("\"", "").Trim();
                        if (!string.IsNullOrEmpty(cellVal))
                        {
                            headerMap[cellVal] = i;
                        }
                    }

                    // Check if all required headers exist
                    var missingHeaders = expectedHeaders.Where(eh => !headerMap.ContainsKey(eh)).ToList();
                    if (missingHeaders.Any())
                    {
                        errors.Add($"Invalid Template: Missing headers: {string.Join(", ", missingHeaders)}");
                        return (0, 0, errors);
                    }

                    // Helper to get value by header name
                    string? GetVal(IXLRangeRow r, string header) => headerMap.TryGetValue(header, out var idx) ? r.Cell(idx).GetValue<string>()?.Trim() : null;
                    object GetRaw(IXLRangeRow r, string header) => headerMap.TryGetValue(header, out var idx) ? r.Cell(idx).Value : Blank.Value;

                    var dataRows = rows.Skip(1).ToList();
                    if (!dataRows.Any())
                    {
                        errors.Add("No valid data rows found in the file.");
                        return (0, 0, errors);
                    }

                    // 2. Pre-fetch dependencies
                    var categoriesList = await _db.Categories
                        .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId || x.BranchId == null))
                        .AsNoTracking().ToListAsync();
                    
                    var categories = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
                    foreach(var c in categoriesList)
                    {
                        if (!string.IsNullOrEmpty(c.CategoryName)) categories[c.CategoryName.Trim()] = c.Id;
                        if (!string.IsNullOrEmpty(c.CategoryCode)) categories[c.CategoryCode.Trim()] = c.Id;
                    }
                    
                    var subcategoriesList = await _db.Subcategories
                        .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId || x.BranchId == null))
                        .ToListAsync();
                    
                    var warehousesList = await _db.Warehouses
                        .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId || x.BranchId == null))
                        .ToListAsync();
                    
                    var racks = await _db.Racks
                        .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId || x.BranchId == null))
                        .ToListAsync();
                    
                    // 3. Pre-fetch existing products (company-wide, bypassing branch restrictions)
                    var dbProducts = await _db.Products
                        .IgnoreQueryFilters()
                        .Where(x => x.CompanyId == companyId)
                        .ToListAsync();
                    var dbProductsByName = dbProducts.GroupBy(p => (p.Name ?? "").ToLower().Trim()).ToDictionary(g => g.Key, g => g.First());
                    
                    var dbProductsBySku = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
                    foreach(var p in dbProducts.Where(p => !string.IsNullOrEmpty(p.Sku)))
                    {
                        dbProductsBySku[p.Sku!.Trim()] = p;
                    }

                    var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var fileSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    // 4. PROCESS ROWS
                    foreach (var row in dataRows)
                    {
                        int rowNum = row.RowNumber();
                        try
                        {
                            var catName = GetVal(row, "Category");
                            var subName = GetVal(row, "Subcategory");
                            var name = GetVal(row, "ProductName");
                            var sku = GetVal(row, "SKU");
                            var brand = GetVal(row, "Brand");
                            var unit = GetVal(row, "Unit");
                            
                            var basePriceVal = GetRaw(row, "BasePrice");
                            var mrpVal = GetRaw(row, "MRP");
                            var discountVal = GetRaw(row, "Discount");
                            var saleRateVal = GetRaw(row, "SaleRate");
                            var gstVal = GetRaw(row, "GST%");
                            var hsn = GetVal(row, "HSNCode");
                            
                            var minStockVal = GetRaw(row, "MinStock");
                            var damagedStockVal = GetRaw(row, "DamagedStock");
                            var pType = GetVal(row, "ProductType");
                            
                            var trackInvRaw = GetVal(row, "TrackInventory")?.ToLower();
                            var trackInv = trackInvRaw == "true" || trackInvRaw == "yes" || trackInvRaw == "1";
                            
                            var reqExpiryRaw = GetVal(row, "RequiresExpiry")?.ToLower();
                            var reqExpiry = reqExpiryRaw == "true" || reqExpiryRaw == "yes" || reqExpiryRaw == "1";
                            
                            var activeRaw = GetVal(row, "Active")?.ToLower();
                            var active = string.IsNullOrWhiteSpace(activeRaw) || activeRaw == "true" || activeRaw == "yes" || activeRaw == "1" || activeRaw == "active";

                            var whName = GetVal(row, "DefaultWarehouse");
                            var rackName = GetVal(row, "DefaultRack");
                            var desc = GetVal(row, "Description");

                            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(catName)) continue;

                            if (string.IsNullOrWhiteSpace(name)) { errors.Add($"Row {rowNum}: ProductName is required."); continue; }
                            if (string.IsNullOrWhiteSpace(catName)) { errors.Add($"Row {rowNum}: Category is required."); continue; }
                            if (string.IsNullOrWhiteSpace(subName)) { errors.Add($"Row {rowNum}: Subcategory is required."); continue; }
                            if (string.IsNullOrWhiteSpace(unit)) { errors.Add($"Row {rowNum}: Unit is required."); continue; }
                            if (string.IsNullOrWhiteSpace(hsn)) { errors.Add($"Row {rowNum}: HSN Code is required."); continue; }

                            if (!categories.TryGetValue(catName.Trim(), out var catId))
                            {
                                errors.Add($"Row {rowNum}: Category '{catName}' not found."); continue;
                            }

                            var subInfo = subcategoriesList.FirstOrDefault(s => 
                                ((s.SubcategoryName ?? "").Equals(subName, StringComparison.OrdinalIgnoreCase) || 
                                 (s.SubcategoryCode ?? "").Equals(subName, StringComparison.OrdinalIgnoreCase)) 
                                && s.CategoryId == catId);

                            if (subInfo == null)
                            {
                                decimal defaultGst = 18;
                                if (gstVal != null)
                                {
                                    var subGstStr = gstVal.ToString()?.Replace("%", "").Trim();
                                    decimal.TryParse(subGstStr, out defaultGst);
                                }

                                var subCode = subName.Trim().ToUpper().Replace(" ", "");
                                if (subCode.Length > 20) subCode = subCode.Substring(0, 20);

                                subInfo = new Subcategory(
                                    categoryid: catId,
                                    code: subCode,
                                    name: subName.Trim(),
                                    defaultGst: defaultGst,
                                    description: "Auto-created during bulk upload",
                                    isactive: true,
                                    companyId: companyId,
                                    branchId: branchId
                                );

                                _db.Subcategories.Add(subInfo);
                                subcategoriesList.Add(subInfo);
                            }

                            Guid? warehouseId = null;
                            if (!string.IsNullOrWhiteSpace(whName))
                            {
                                var whInfo = warehousesList.FirstOrDefault(w => (w.Name ?? "").Equals(whName.Trim(), StringComparison.OrdinalIgnoreCase));
                                if (whInfo != null)
                                {
                                    warehouseId = whInfo.Id;
                                }
                                else
                                {
                                    var newWh = new Warehouse(
                                        name: whName.Trim(),
                                        city: "Auto-created",
                                        description: "Auto-created during bulk upload",
                                        isActive: true,
                                        companyId: companyId,
                                        branchId: branchId
                                    );

                                    _db.Warehouses.Add(newWh);
                                    warehousesList.Add(newWh);
                                    warehouseId = newWh.Id;
                                }
                            }

                            Guid? rackId = null;
                            if (!string.IsNullOrWhiteSpace(rackName) && warehouseId.HasValue)
                            {
                                var rInfo = racks.FirstOrDefault(r => (r.Name ?? "").Equals(rackName, StringComparison.OrdinalIgnoreCase) && r.WarehouseId == warehouseId);
                                if (rInfo != null)
                                {
                                    rackId = rInfo.Id;
                                }
                                else
                                {
                                    var newRack = new Rack(
                                        warehouseId: warehouseId.Value,
                                        name: rackName.Trim(),
                                        description: "Auto-created during bulk upload",
                                        isActive: true,
                                        companyId: companyId,
                                        branchId: branchId
                                    );
                                    
                                    _db.Racks.Add(newRack);
                                    racks.Add(newRack);
                                    rackId = newRack.Id;
                                }
                            }

                            if (fileNames.Contains(name.Trim())) { errors.Add($"Row {rowNum}: Duplicate Product Name '{name}' in file."); continue; }
                            if (!string.IsNullOrEmpty(sku) && fileSkus.Contains(sku.Trim())) { errors.Add($"Row {rowNum}: Duplicate SKU '{sku}' in file."); continue; }
                            
                            fileNames.Add(name.Trim());
                            if (!string.IsNullOrEmpty(sku)) fileSkus.Add(sku.Trim());

                            decimal basePrice = 0, mrp = 0, discount = 0, saleRate = 0, gst = 0, damagedStock = 0;
                            int minStock = 0;

                            decimal.TryParse(basePriceVal.ToString()?.Replace(",", ""), out basePrice);
                            decimal.TryParse(mrpVal.ToString()?.Replace(",", ""), out mrp);
                            decimal.TryParse(discountVal.ToString()?.Replace(",", ""), out discount);
                            decimal.TryParse(saleRateVal.ToString()?.Replace(",", ""), out saleRate);
                            
                            var gstStr = gstVal.ToString()?.Replace("%", "").Trim();
                            decimal.TryParse(gstStr, out gst);
                            
                            decimal.TryParse(damagedStockVal.ToString()?.Replace(",", ""), out damagedStock);
                            
                            if (decimal.TryParse(minStockVal.ToString(), out var minStockDec))
                            {
                                minStock = (int)Math.Round(minStockDec);
                            }

                            string mappedType = pType?.ToLower() switch
                            {
                                "finished" => "1",
                                "goods" => "2",
                                "raw material" => "3",
                                _ => "1"
                            };

                            Product? existingProduct = null;
                            if (!string.IsNullOrEmpty(sku) && dbProductsBySku.TryGetValue(sku.Trim(), out var pBySku))
                            {
                                existingProduct = pBySku;
                            }
                            else if (dbProductsByName.TryGetValue(name.Trim().ToLower(), out var pByName))
                            {
                                existingProduct = pByName;
                            }

                            if (existingProduct != null && !string.IsNullOrEmpty(sku) && !sku.Equals(existingProduct.Sku, StringComparison.OrdinalIgnoreCase))
                            {
                                if (dbProductsBySku.TryGetValue(sku.Trim(), out var otherProductWithSameSku) && otherProductWithSameSku.Id != existingProduct.Id)
                                {
                                    errors.Add($"Row {rowNum}: Cannot update product '{name}' to SKU '{sku}' because SKU '{sku}' is already assigned to another product '{otherProductWithSameSku.Name}'.");
                                    continue;
                                }
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
                                    branchId: existingProduct.BranchId ?? branchId
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
                            errors.Add($"Row {rowNum}: Error - {ex.Message}");
                        }
                    }

                    if (successCount > 0 || updateCount > 0)
                    {
                        try
                        {
                            await _db.SaveChangesAsync();
                        }
                        catch (Exception dbEx)
                        {
                            successCount = 0;
                            updateCount = 0;
                            errors.Add($"Database Save Error: {dbEx.InnerException?.Message ?? dbEx.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            successCount = 0;
            updateCount = 0;
            errors.Add($"Fatal Error: {ex.Message}. Make sure you are using a valid .xlsx file.");
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
