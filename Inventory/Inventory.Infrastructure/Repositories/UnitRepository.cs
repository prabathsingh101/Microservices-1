using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories
{
    public class UnitRepository : IUnitRepository
    {
        private readonly InventoryDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public UnitRepository(InventoryDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task AddAsync(UnitMaster unit) => await _context.Units.AddAsync(unit);

        public async Task DeleteAsync(Guid id)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);
            if (unit != null)
            {
                _context.Units.Remove(unit);
            }
        }

        public async Task<IEnumerable<UnitMaster>> GetAllAsync()
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            return await _context.Units
                         .AsNoTracking()
                         .Where(x => x.CompanyId == companyId)
                         .ToListAsync();
        }

        public async Task<UnitMaster> GetByIdAsync(Guid id) 
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            return await _context.Units.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);
        }

        public Task UpdateAsync(UnitMaster unit)
        {
            _context.Units.Update(unit);
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string name)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            return await _context.Units.AnyAsync(u => u.Name.ToLower() == name.ToLower() && u.CompanyId == companyId);
        }

        public IQueryable<UnitMaster> Query() 
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            return _context.Units.Where(x => x.CompanyId == companyId).AsQueryable();
        }
    }
}
