using Inventory.Domain.Entities;
using Microsoft.AspNetCore.Http;

public interface ICategoryRepository
{
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(Category category);

    Task<List<Category>> GetAllAsync();

    Task<Category?> GetByIdAsync(Guid id);
    Task<List<Category>> GetByIdsAsync(List<Guid> ids);

    Task<bool> HasSubcategoriesAsync(Guid categoryId);
    Task<bool> HasSubcategoriesAsync(List<Guid> categoryIds);

    void Delete(Category category);
    void DeleteRange(List<Category> categories);

    IQueryable<Category> Query();

    Task<(int successCount, int updateCount, List<string> errors)> UploadCategoriesAsync(IFormFile file, Guid companyId);
    Task<bool> ExistsByNameAsync(string name, Guid companyId, Guid? excludeId = null);
}
