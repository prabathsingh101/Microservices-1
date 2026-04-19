using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
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
        return await _context.Warehouses
            .Where(x => x.CompanyId == companyId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Warehouse?> GetByIdAsync(Guid id)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _context.Warehouses
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId);
    }
}
