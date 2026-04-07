using Inventory.Domain.Entities;

namespace Inventory.Application.Common.Interfaces;

public interface IUnitRepository
{
    Task<UnitMaster> GetByIdAsync(int id);
    Task<IEnumerable<UnitMaster>> GetAllAsync();
    Task AddAsync(UnitMaster unit);
    Task UpdateAsync(UnitMaster unit);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(string name);
    IQueryable<UnitMaster> Query();
}