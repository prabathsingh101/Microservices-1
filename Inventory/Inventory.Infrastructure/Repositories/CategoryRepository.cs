using ClosedXML.Excel;
using Inventory.Application.Common.Interfaces;
using DocumentFormat.OpenXml.InkML;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly InventoryDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public CategoryRepository(InventoryDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task AddAsync(Category category)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category category)
    {
        _db.Categories.Update(category);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Category category)
    {
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _db.Categories.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId);
    }

    public async Task<List<Category>> GetAllAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _db.Categories
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .ToListAsync();
    }
    

    public async Task<List<Category>> GetByIdsAsync(List<Guid> ids)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _db.Categories
            .Where(x => ids.Contains(x.Id) && x.CompanyId == companyId)
            .ToListAsync();
    }

    public void DeleteRange(List<Category> categories)
    {
        _db.Categories.RemoveRange(categories);
    }

    // ✅ DEPENDENCY CHECK (NO NAVIGATION PROPERTY)
    public async Task<bool> HasSubcategoriesAsync(Guid categoryId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _db.Subcategories
            .AnyAsync(x => x.CategoryId == categoryId && x.CompanyId == companyId);
    }

    public async Task<bool> HasSubcategoriesAsync(List<Guid> categoryIds)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _db.Subcategories
            .AnyAsync(x => categoryIds.Contains(x.CategoryId) && x.CompanyId == companyId);
    }

    public void Delete(Category category)
    {
        _db.Categories.Remove(category);
    }
    public IQueryable<Category> Query()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return _db.Categories.Where(x => x.CompanyId == companyId).AsQueryable();
    }

        public async Task<(int successCount, int updateCount, List<string> errors)> UploadCategoriesAsync(IFormFile file, Guid companyId, string? branchId = null)
        {
            var errors = new List<string>();
            int successCount = 0;
            int updateCount = 0;

        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            using (var workbook = new XLWorkbook(stream))
            {
                var worksheet = workbook.Worksheet(1); // First sheet
                var rows = worksheet.RangeUsed().RowsUsed();

                // 1. Header Validation
                var headerRow = rows.FirstOrDefault();
                if (headerRow == null)
                {
                    errors.Add("Invalid Template: File is empty.");
                    return (0, 0, errors);
                }

                var expectedHeaders = new List<string> { "CategoryCode", "CategoryName", "DefaultGst", "Description" };
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
                    errors.Add($"Invalid Template: Headers do not match. Expected: {string.Join(", ", expectedHeaders)}");
                    return (0, 0, errors);
                }

                var dataRows = rows.Skip(1);

                // 2. Pre-fetch Categories FOR THIS COMPANY ONLY
                var dbCategories = await _db.Categories
                    .Where(x => x.CompanyId == companyId)
                    .ToListAsync();

                var dbCatsByCode = dbCategories.GroupBy(c => c.CategoryCode?.ToLower().Trim() ?? "").ToDictionary(g => g.Key, g => g.First());
                
                // Track by name as well (only for active ones to match existing logic)
                var dbCatsByName = new Dictionary<string, Category>();
                foreach(var c in dbCategories.Where(x => x.IsActive))
                {
                    var nameKey = c.CategoryName?.ToLower().Trim() ?? "";
                    if (!string.IsNullOrEmpty(nameKey) && !dbCatsByName.ContainsKey(nameKey)) 
                        dbCatsByName.Add(nameKey, c);
                }

                var newCategories = new List<Category>();

                // 3. In-File Duplicate Check
                var fileCodes = new HashSet<string>();
                var fileNames = new HashSet<string>();

                foreach (var row in dataRows)
                {
                     int rowNum = row.RowNumber();
                     try 
                     {
                        var code = row.Cell(1).Value.ToString()?.Trim();
                        var name = row.Cell(2).Value.ToString()?.Trim();
                        var gstValue = row.Cell(3).Value;
                        var description = row.Cell(4).Value.ToString()?.Trim();

                        // Skip Empty Rows (Strict)
                        if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name))
                        {
                            continue;
                        }

                        // Validation
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            errors.Add($"Row {rowNum}: Category Name is required.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(code))
                        {
                            errors.Add($"Row {rowNum}: Category Code is required.");
                            continue;
                        }

                        // Duplicate Check (In-File)
                        if (fileCodes.Contains(code?.ToLower().Trim() ?? ""))
                        {
                            errors.Add($"Row {rowNum}: Duplicate Code '{code}' in file.");
                            continue;
                        }
                        if (fileNames.Contains(name?.ToLower().Trim() ?? ""))
                        {
                            errors.Add($"Row {rowNum}: Duplicate Name '{name}' in file.");
                            continue;
                        }

                        fileCodes.Add(code?.ToLower().Trim() ?? "");
                        fileNames.Add(name?.ToLower().Trim() ?? "");

                        // GST Parsing
                        decimal defaultGst = 0;
                        if (!gstValue.IsBlank)
                        {
                             if (!decimal.TryParse(gstValue.ToString(), out defaultGst))
                             {
                                 errors.Add($"Row {rowNum}: Invalid GST '{gstValue}'.");
                                 continue;
                             }
                        }

                        // 4. UPSERT LOGIC
                        Category? existingCategory = null;

                        // Priority 1: Check by Code
                        if (dbCatsByCode.TryGetValue(code?.ToLower().Trim() ?? "", out var catByCode))
                        {
                            existingCategory = catByCode;
                        }
                        // Priority 2: Check by Name
                        else if (dbCatsByName.TryGetValue(name?.ToLower().Trim() ?? "", out var catByName))
                        {
                            existingCategory = catByName;
                        }

                        if (existingCategory != null)
                        {
                            // UPDATE EXISTING
                            existingCategory.Update(
                                name: name,
                                code: code,
                                defaultGst: defaultGst,
                                description: description,
                                isActive: true, 
                                companyId: companyId, // Explicitly pass CompanyId
                                parentCategoryId: null,
                                branchId: branchId
                            );
                            updateCount++;
                        }
                        else
                        {
                            // INSERT NEW
                            var category = new Category(
                                name,
                                code,
                                defaultGst,
                                description,
                                true, // IsActive
                                companyId, // Mandatory CompanyId
                                null, // ParentCategoryId
                                branchId
                            );
                            newCategories.Add(category);
                            successCount++;
                        }
                     }
                     catch(Exception ex)
                     {
                         errors.Add($"Row {rowNum}: Fatal error - {ex.Message}");
                     }
                }

                if (!newCategories.Any() && updateCount == 0 && !errors.Any())
                {
                    errors.Add("No valid data rows found in the file.");
                }

                if (newCategories.Any() || updateCount > 0)
                {
                    if (newCategories.Any()) await _db.Categories.AddRangeAsync(newCategories);
                    await _db.SaveChangesAsync();
                }
            }
        }
        return (successCount, updateCount, errors);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid companyId, Guid? excludeId = null)
    {
        var query = _db.Categories.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.CategoryName.ToLower().Trim() == name.ToLower().Trim() && x.IsActive);

        if (excludeId.HasValue && excludeId != Guid.Empty)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}
