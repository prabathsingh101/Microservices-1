using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
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
}
