using Inventory.Domain.Entities;

namespace Inventory.Application.Common.Interfaces;

public interface IWarehouseRepository
{
    Task AddAsync(Warehouse warehouse);
    Task UpdateAsync(Warehouse warehouse);
    Task DeleteAsync(Warehouse warehouse);
    Task<List<Warehouse>> GetAllAsync();
    Task<Warehouse?> GetByIdAsync(Guid id);
    Task<(int successCount, List<string> errors)> UploadWarehousesAsync(Microsoft.AspNetCore.Http.IFormFile file, Guid companyId);
}
