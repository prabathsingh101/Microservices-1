using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.IO;

internal sealed class SubcategoryRepository : ISubcategoryRepository
{
    private readonly InventoryDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SubcategoryRepository(InventoryDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
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
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _context.Subcategories
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId);
    }

    public async Task<List<Subcategory>> GetAllAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _context.Subcategories
            .Include(s => s.Category)
            .Where(x => x.CompanyId == companyId)
            .ToListAsync();
    }

    public async Task<List<Subcategory>> GetByCategoryIdAsync(Guid categoryId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _context.Subcategories
            .Include(s => s.Category)
            .Where(s => s.CategoryId == categoryId && s.CompanyId == companyId)
            .ToListAsync();
    }

    public IQueryable<Subcategory> Query()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return _context.Subcategories.Where(x => x.CompanyId == companyId).AsQueryable();
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
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _context.Subcategories
            .Where(x => ids.Contains(x.Id) && x.CompanyId == companyId)
            .ToListAsync();
    }


    // ✅ DEPENDENCY CHECK (NO NAVIGATION PROPERTY)
    public async Task<bool> HasSubcategoriesAsync(Guid categoryId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _context.Subcategories
            .AnyAsync(x => x.CategoryId == categoryId && x.CompanyId == companyId);
    }

    public async Task<bool> HasSubcategoriesAsync(List<Guid> categoryIds)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _context.Subcategories
            .AnyAsync(x => categoryIds.Contains(x.CategoryId) && x.CompanyId == companyId);
    }

    public async Task<(int successCount, int updateCount, List<string> errors)> UploadSubcategoriesAsync(Microsoft.AspNetCore.Http.IFormFile file, Guid companyId)
    {
        var errors = new List<string>();
        int successCount = 0;
        int updateCount = 0;

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
                    return (0, 0, errors);
                }

                var expectedHeaders = new List<string> { "SubcategoryCode", "CategoryName", "SubcategoryName", "DefaultGst", "Description" };
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

                // 2. Pre-fetch ALL Categories for lookup (Case-insensitive) by Name
                var categories = await _context.Categories
                    .Where(x => x.CompanyId == companyId)
                    .AsNoTracking()
                    .ToDictionaryAsync(c => (c.CategoryName ?? "").ToLower().Trim(), c => c.Id);

                // 3. Pre-fetch existing Subcategories for Upsert logic (Update if exists, Insert if new)
                // We don't use AsNoTracking() so we can update existing entities
                var dbSubcategories = await _context.Subcategories
                    .Where(x => x.CompanyId == companyId)
                    .ToListAsync();
                var dbSubcatsByCode = dbSubcategories.ToDictionary(s => (s.SubcategoryCode ?? "").ToLower().Trim(), s => s);
                
                // Track by name (active only)
                var dbSubcatsByName = new Dictionary<string, Subcategory>();
                foreach(var s in dbSubcategories.Where(x => x.IsActive))
                {
                    var nameKey = (s.SubcategoryName ?? "").ToLower().Trim();
                    if (!dbSubcatsByName.ContainsKey(nameKey)) dbSubcatsByName.Add(nameKey, s);
                }

                // Track duplicates within the file itself
                var fileCodes = new HashSet<string>();
                var fileNames = new HashSet<string>();
                // 4. PROCESS ROWS
                foreach (var row in dataRows)
                {
                    int rowNum = row.RowNumber();
                    try
                    {
                        var code = row.Cell(1).GetValue<string>()?.Trim();
                        var catNameValue = row.Cell(2).GetValue<string>()?.Trim();
                        var name = row.Cell(3).GetValue<string>()?.Trim();
                        var gstValue = row.Cell(4).Value;
                        var description = row.Cell(5).GetValue<string>()?.Trim();

                        if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(catNameValue) && string.IsNullOrWhiteSpace(name)) continue;
                        if (string.IsNullOrWhiteSpace(name)) { errors.Add($"Row {rowNum}: Name is required."); continue; }
                        if (string.IsNullOrWhiteSpace(code)) { errors.Add($"Row {rowNum}: Code is required."); continue; }
                        if (string.IsNullOrWhiteSpace(catNameValue)) { errors.Add($"Row {rowNum}: Category is required."); continue; }

                        if (!categories.TryGetValue(catNameValue?.ToLower().Trim() ?? "", out var categoryId))
                        {
                            errors.Add($"Row {rowNum}: Category '{catNameValue}' not found.");
                            continue;
                        }

                        if (fileCodes.Contains(code?.ToLower() ?? "")) { errors.Add($"Row {rowNum}: Duplicate Code '{code}' in file."); continue; }
                        if (fileNames.Contains(name?.ToLower() ?? "")) { errors.Add($"Row {rowNum}: Duplicate Name '{name}' in file."); continue; }
                        fileCodes.Add(code?.ToLower() ?? "");
                        fileNames.Add(name?.ToLower() ?? "");

                        decimal defaultGst = 0;
                        if (!gstValue.IsBlank) decimal.TryParse(gstValue.ToString().Replace("%", "").Trim(), out defaultGst);

                        Subcategory? existingSubcat = null;
                        if (dbSubcatsByCode.TryGetValue(code?.ToLower().Trim() ?? "", out var subByCode)) existingSubcat = subByCode;
                        else if (dbSubcatsByName.TryGetValue(name?.ToLower().Trim() ?? "", out var subByName)) existingSubcat = subByName;

                        if (existingSubcat != null)
                        {
                            existingSubcat.Update(code, name, categoryId, defaultGst, description, true, companyId);
                            updateCount++;
                        }
                        else
                        {
                            var subcat = new Subcategory(categoryId, code, name, defaultGst, description, true, companyId);
                            await _context.Subcategories.AddAsync(subcat);
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {rowNum}: {ex.Message}");
                    }
                }
                
                // Batch Save at the end
                if (successCount > 0 || updateCount > 0)
                {
                    await _context.SaveChangesAsync();
                }
            }
        }
        return (successCount, updateCount, errors);
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
