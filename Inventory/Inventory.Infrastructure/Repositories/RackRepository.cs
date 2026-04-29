using ClosedXML.Excel;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

public class RackRepository : IRackRepository
{
    private readonly InventoryDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RackRepository(InventoryDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task AddAsync(Rack rack)
    {
        await _context.Racks.AddAsync(rack);
    }

    public Task UpdateAsync(Rack rack)
    {
        _context.Racks.Update(rack);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Rack rack)
    {
        _context.Racks.Remove(rack);
        return Task.CompletedTask;
    }

    public async Task<List<Rack>> GetAllAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _context.Racks
            .Include(r => r.Warehouse)
            .Where(x => x.CompanyId == companyId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Rack>> GetByWarehouseIdAsync(Guid warehouseId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _context.Racks
            .Where(r => r.WarehouseId == warehouseId && r.CompanyId == companyId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Rack?> GetByIdAsync(Guid id)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _context.Racks
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId);
    }

    public async Task<(int successCount, List<string> errors)> UploadRacksAsync(IFormFile file, Guid companyId, string? branchId = null)
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

                // Pre-fetch for mapping & upsert
                var warehousesList = await _context.Warehouses
                    .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId || x.BranchId == null))
                    .ToListAsync();
                var warehouses = warehousesList.GroupBy(w => w.Name.ToLower().Trim()).ToDictionary(g => g.Key, g => g.First().Id);

                var dbRacks = await _context.Racks
                    .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId || x.BranchId == null))
                    .ToListAsync();

                var newRacks = new List<Rack>();
                int updateCount = 0;

                foreach (var row in dataRows)
                {
                    int rowNum = row.RowNumber();
                    try
                    {
                        var whName = row.Cell(1).Value.ToString()?.Trim();
                        var rackName = row.Cell(2).Value.ToString()?.Trim();
                        var description = row.Cell(3).Value.ToString()?.Trim();
                        
                        var activeValue = row.Cell(4).Value.ToString()?.Trim();
                        bool isActive = string.IsNullOrEmpty(activeValue) || 
                                        activeValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || 
                                        activeValue.Equals("1") || 
                                        activeValue.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);

                        if (string.IsNullOrWhiteSpace(rackName) || string.IsNullOrWhiteSpace(whName)) continue;

                        if (!warehouses.TryGetValue(whName.ToLower(), out var warehouseId))
                        {
                            errors.Add($"Row {rowNum}: Warehouse '{whName}' not found.");
                            continue;
                        }

                        // Use the branchId passed from the controller instead of claim in the loop if available
                        var currentBranchId = branchId ?? _currentUserService.BranchId;

                        var existing = dbRacks.FirstOrDefault(r => r.Name.ToLower().Trim() == rackName.ToLower() && r.WarehouseId == warehouseId);

                        if (existing != null)
                        {
                            existing.Update(warehouseId, rackName, description, isActive, companyId, currentBranchId);
                            updateCount++;
                        }
                        else
                        {
                            var rack = new Rack(warehouseId, rackName, description, isActive, companyId, currentBranchId);
                            newRacks.Add(rack);
                        }
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {rowNum}: Error - {ex.Message}");
                    }
                }

                if (newRacks.Any() || updateCount > 0)
                {
                    if (newRacks.Any()) await _context.Racks.AddRangeAsync(newRacks);
                    await _context.SaveChangesAsync();
                }
            }
        }
        return (successCount, errors);
    }
}
