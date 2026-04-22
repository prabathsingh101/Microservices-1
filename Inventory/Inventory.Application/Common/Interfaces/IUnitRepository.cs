using Inventory.Domain.Entities;

namespace Inventory.Application.Common.Interfaces;

public interface IUnitRepository
{
    Task<UnitMaster> GetByIdAsync(Guid id);
    Task<IEnumerable<UnitMaster>> GetAllAsync();
    Task AddAsync(UnitMaster unit);
    Task UpdateAsync(UnitMaster unit);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(string name);
    IQueryable<UnitMaster> Query();
    Task<(int successCount, List<string> errors)> UploadUnitsAsync(Microsoft.AspNetCore.Http.IFormFile file, Guid companyId);
}
