using Inventory.Application.Products.DTOs;
using Inventory.Application.Stock;
using Inventory.Domain.Entities;
using Inventory.Domain.PriceLists;

namespace Inventory.Application.Common.Interfaces;

public interface IProductRepository
{
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);

    Task<Product?> GetByIdAsync(Guid id);
    Task<List<Product>> GetAllAsync();

    Task<List<Product>> GetByCategoryIdAsync(Guid categoryId);
    Task<List<Product>> GetBySubcategoryIdAsync(Guid subcategoryId);

    IQueryable<Product> Query();
    void DeleteRange(List<Product> product);
    Task<List<Product>> GetByIdsAsync(List<Guid> ids);
    Task<bool> HasPriceListAsync(List<Guid> productIds);

    Task<List<Product>> SearchActiveProductsAsync(string term);
    Task<ProductRateDto> GetProductRateAsync(Guid productId, Guid? priceListId);

    Task<IEnumerable<LowStockProductDto>> GetLowStockProductsAsync();

    Task<List<ExcelExportDto>> GetLowStockExportDataAsync();

    Task<List<StockMovementDto>> GetRecentMovementsPagedAsync(int pageNumber, int pageSize);

    Task<(int successCount, List<string> errors)> UploadProductsAsync(Microsoft.AspNetCore.Http.IFormFile file, Guid companyId);
    Task<bool> ExistsByNameAsync(string name, Guid companyId, Guid? excludeId = null);
}
