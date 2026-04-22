using Inventory.Domain.Entities;

namespace Inventory.Application.Common.Interfaces;

public interface IRackRepository
{
    Task AddAsync(Rack rack);
    Task UpdateAsync(Rack rack);
    Task DeleteAsync(Rack rack);
    Task<List<Rack>> GetAllAsync();
    Task<List<Rack>> GetByWarehouseIdAsync(Guid warehouseId);
    Task<Rack?> GetByIdAsync(Guid id);
    Task<(int successCount, List<string> errors)> UploadRacksAsync(Microsoft.AspNetCore.Http.IFormFile file, Guid companyId);
}
