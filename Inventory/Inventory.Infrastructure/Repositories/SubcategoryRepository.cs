using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.IO;

internal sealed class SubcategoryRepository : ISubcategoryRepository
{
    private readonly InventoryDbContext _context;

    public SubcategoryRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Subcategory subcategory)
    {
        await _context.Subcategories.AddAsync(subcategory);
    }

    public Task UpdateAsync(Subcategory subcategory)
    {
        _context.Subcategories.Update(subcategory);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Subcategory subcategory)
    {
        _context.Subcategories.Remove(subcategory);
        return Task.CompletedTask;
    }

    public async Task<Subcategory?> GetByIdAsync(Guid id)
    {
        return await _context.Subcategories
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<Subcategory>> GetAllAsync()
    {
        return await _context.Subcategories
            .Include(s=>s.Category)
            .ToListAsync();
    }

    public async Task<List<Subcategory>> GetByCategoryIdAsync(Guid categoryId)
    {
        return await _context.Subcategories
            .Include(s => s.Category)
            .Where(s => s.CategoryId == categoryId)
            .ToListAsync();
    }

    public IQueryable<Subcategory> Query()
    {
        return _context.Subcategories.AsQueryable();
    }

    public void Delete(Subcategory subcategory)
    {
        _context.Subcategories.Remove(subcategory);
    }

    public void DeleteRange(List<Subcategory> subcategories)
    {
        _context.Subcategories.RemoveRange(subcategories);
    }

    public async Task<List<Subcategory>> GetByIdsAsync(List<Guid> ids)
    {
        return await _context.Subcategories
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();
    }  

    // ✅ DEPENDENCY CHECK (NO NAVIGATION PROPERTY)
    public async Task<bool> HasSubcategoriesAsync(Guid categoryId)
    {
        return await _context.Subcategories
            .AnyAsync(x => x.CategoryId == categoryId);
    }

    public async Task<bool> HasSubcategoriesAsync(List<Guid> categoryIds)
    {
        return await _context.Subcategories
            .AnyAsync(x => categoryIds.Contains(x.CategoryId));
    }

    public async Task<(int successCount, List<string> errors)> UploadSubcategoriesAsync(Microsoft.AspNetCore.Http.IFormFile file, Guid companyId)
    {
        var errors = new List<string>();
        int successCount = 0;

        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            using (var workbook = new ClosedXML.Excel.XLWorkbook(stream))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed();
                
                // 1. Header Validation relative to the user's template
                var headerRow = rows.FirstOrDefault();
                if (headerRow == null)
                {
                    errors.Add("Invalid Template: File is empty.");
                    return (0, errors);
                }

                var expectedHeaders = new List<string> { "SubcategoryCode", "CategoryName", "SubcategoryName", "DefaultGst", "Description" };
                var actualHeaders = new List<string>();
                
                // Check first 5 columns
                for(int i = 1; i <= 5; i++)
                {
                    actualHeaders.Add(headerRow.Cell(i).GetValue<string>().Trim());
                }

                if (!expectedHeaders.SequenceEqual(actualHeaders))
                {
                    errors.Add($"Invalid Template: Headers do not match. Expected: {string.Join(", ", expectedHeaders)}");
                    return (0, errors);
                }

                var dataRows = rows.Skip(1); 

                // 2. Pre-fetch ALL Categories for lookup (Case-insensitive) by Name
                var categories = await _context.Categories
                    .Where(x => x.CompanyId == companyId)
                    .AsNoTracking()
                    .ToDictionaryAsync(c => c.CategoryName.ToLower().Trim(), c => c.Id);

                // 3. Pre-fetch existing Subcategories for Upsert logic (Update if exists, Insert if new)
                // We don't use AsNoTracking() so we can update existing entities
                var dbSubcategories = await _context.Subcategories
                    .Where(x => x.CompanyId == companyId)
                    .ToListAsync();
                var dbSubcatsByCode = dbSubcategories.ToDictionary(s => s.SubcategoryCode.ToLower().Trim(), s => s);
                
                // Track by name (active only)
                var dbSubcatsByName = new Dictionary<string, Subcategory>();
                foreach(var s in dbSubcategories.Where(x => x.IsActive))
                {
                    var nameKey = s.SubcategoryName.ToLower().Trim();
                    if (!dbSubcatsByName.ContainsKey(nameKey)) dbSubcatsByName.Add(nameKey, s);
                }

                var newSubcategories = new List<Subcategory>();
                int updateCount = 0;
                
                // Track duplicates within the file itself
                var fileCodes = new HashSet<string>();
                var fileNames = new HashSet<string>();

                foreach (var row in dataRows)
                {
                    int rowNum = row.RowNumber();
                    try
                    {
                        var code = row.Cell(1).Value.ToString()?.Trim();
                        var catNameValue = row.Cell(2).Value.ToString()?.Trim();
                        var name = row.Cell(3).Value.ToString()?.Trim();
                        var gstValue = row.Cell(4).Value;
                        var description = row.Cell(5).Value.ToString()?.Trim();

                        // Skip empty rows (strictly)
                        if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(catNameValue) && string.IsNullOrWhiteSpace(name)) 
                        {
                            continue;
                        }

                        // Validation
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            errors.Add($"Row {rowNum}: Subcategory Name is empty.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(code))
                        {
                            errors.Add($"Row {rowNum}: Subcategory Code is empty.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(catNameValue))
                        {
                            errors.Add($"Row {rowNum}: Category Name is empty.");
                            continue;
                        }

                        // Category Lookup
                        if (!categories.TryGetValue(catNameValue.ToLower(), out var categoryId))
                        {
                            errors.Add($"Row {rowNum}: Category '{catNameValue}' not found.");
                            continue;
                        }

                        // Duplicate Check (In-File)
                        if (fileCodes.Contains(code.ToLower()))
                        {
                            errors.Add($"Row {rowNum}: Duplicate Code '{code}' in file.");
                            continue;
                        }
                        if (fileNames.Contains(name.ToLower()))
                        {
                            errors.Add($"Row {rowNum}: Duplicate Name '{name}' in file.");
                            continue;
                        }

                        fileCodes.Add(code.ToLower());
                        fileNames.Add(name.ToLower());

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
                        Subcategory? existingSubcat = null;

                        // Priority 1: Check by Code
                        if (dbSubcatsByCode.TryGetValue(code.ToLower(), out var subByCode))
                        {
                            existingSubcat = subByCode;
                        }
                        // Priority 2: Check by Name
                        else if (dbSubcatsByName.TryGetValue(name.ToLower(), out var subByName))
                        {
                            existingSubcat = subByName;
                        }

                        if (existingSubcat != null)
                        {
                            // UPDATE EXISTING
                            existingSubcat.Update(
                                code: code,
                                name: name,
                                categoryid: categoryId,
                                defaultgst: defaultGst,
                                description: description,
                                isActive: true,
                                companyId: companyId
                            );
                            updateCount++;
                        }
                        else
                        {
                            // INSERT NEW
                            var subcategory = new Subcategory(
                                categoryId,
                                code,
                                name,
                                defaultGst,
                                description,
                                true, // Active by default
                                companyId
                            );
                            newSubcategories.Add(subcategory);
                        }
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {rowNum}: Fatal Error - {ex.Message}");
                    }
                }

                if (!newSubcategories.Any() && updateCount == 0 && !errors.Any())
                {
                    errors.Add("No valid data rows found in the file.");
                }

                if (newSubcategories.Any() || updateCount > 0)
                {
                    if (newSubcategories.Any()) await _context.Subcategories.AddRangeAsync(newSubcategories);
                    await _context.SaveChangesAsync();
                }
            }
        }

        return (successCount, errors);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid companyId, Guid? excludeId = null)
    {
        var query = _context.Subcategories.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.SubcategoryName.ToLower().Trim() == name.ToLower().Trim() && x.IsActive);

        if (excludeId.HasValue && excludeId != Guid.Empty)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}
