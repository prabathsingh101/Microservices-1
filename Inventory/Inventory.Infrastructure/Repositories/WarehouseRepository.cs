using ClosedXML.Excel;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly InventoryDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public WarehouseRepository(InventoryDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task AddAsync(Warehouse warehouse)
    {
        await _context.Warehouses.AddAsync(warehouse);
    }

    public Task UpdateAsync(Warehouse warehouse)
    {
        _context.Warehouses.Update(warehouse);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Warehouse warehouse)
    {
        _context.Warehouses.Remove(warehouse);
        return Task.CompletedTask;
    }

    public async Task<List<Warehouse>> GetAllAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        return await _context.Warehouses
            .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Warehouse?> GetByIdAsync(Guid id)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        var branchId = _currentUserService.BranchId;
        return await _context.Warehouses
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId));
    }

    public async Task<(int successCount, List<string> errors)> UploadWarehousesAsync(IFormFile file, Guid companyId)
    {
        int successCount = 0;
        var errors = new List<string>();

        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            using (var workbook = new XLWorkbook(stream))
            {
                var worksheet = workbook.Worksheets.First();
                var dataRows = worksheet.RowsUsed().Skip(1); // Skip Header

                // Pre-fetch for Upsert
                var dbWarehouses = await _context.Warehouses
                    .Where(x => x.CompanyId == companyId)
                    .ToListAsync();
                var dbWarehousesByName = dbWarehouses.ToDictionary(w => w.Name.ToLower().Trim(), w => w);

                var newWarehouses = new List<Warehouse>();
                int updateCount = 0;

                foreach (var row in dataRows)
                {
                    int rowNum = row.RowNumber();
                    try
                    {
                        var name = row.Cell(1).Value.ToString()?.Trim();
                        var city = row.Cell(2).Value.ToString()?.Trim();
                        var description = row.Cell(3).Value.ToString()?.Trim();
                        
                        var activeValue = row.Cell(4).Value.ToString()?.Trim();
                        bool isActive = string.IsNullOrEmpty(activeValue) || 
                                        activeValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || 
                                        activeValue.Equals("1") || 
                                        activeValue.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);

                        if (string.IsNullOrWhiteSpace(name)) continue;

                        var branchId = _currentUserService.BranchId;

                        if (dbWarehousesByName.TryGetValue(name.ToLower(), out var existing))
                        {
                            existing.Update(name, city, description, isActive, companyId, branchId);
                            updateCount++;
                        }
                        else
                        {
                            var warehouse = new Warehouse(name, city, description, isActive, companyId, branchId);
                            newWarehouses.Add(warehouse);
                        }
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {rowNum}: Error - {ex.Message}");
                    }
                }

                if (newWarehouses.Any() || updateCount > 0)
                {
                    if (newWarehouses.Any()) await _context.Warehouses.AddRangeAsync(newWarehouses);
                    await _context.SaveChangesAsync();
                }
            }
        }
        return (successCount, errors);
    }
}
