using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories
{
    public class UnitRepository : IUnitRepository
    {
        private readonly InventoryDbContext _context;
        public UnitRepository(InventoryDbContext context) => _context = context;

        public async Task AddAsync(UnitMaster unit) => await _context.Units.AddAsync(unit);

        public async Task DeleteAsync(int id)
        {
            var unit = await _context.Units.FindAsync(id);
            if (unit != null)
            {
                _context.Units.Remove(unit);
            }
        }

        public async Task<IEnumerable<UnitMaster>> GetAllAsync()
        {
            return await _context.Units
                         .AsNoTracking()
                         .ToListAsync();
        }

        public async Task<UnitMaster> GetByIdAsync(int id) => await _context.Units.FindAsync(id);

        public Task UpdateAsync(UnitMaster unit)
        {
            _context.Units.Update(unit);
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string name)
        {
            return await _context.Units.AnyAsync(u => u.Name.ToLower() == name.ToLower());
        }

        public IQueryable<UnitMaster> Query() => _context.Units.AsQueryable();
    }
}
