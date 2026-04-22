using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Common.Interfaces
{
    public interface ISubcategoryRepository
    {
        Task AddAsync(Subcategory subcategory);
        Task UpdateAsync(Subcategory subcategory);
        Task DeleteAsync(Subcategory subcategory);

        Task<Subcategory?> GetByIdAsync(Guid id);
        Task<List<Subcategory>> GetAllAsync();

        Task<List<Subcategory>> GetByCategoryIdAsync(Guid categoryId);

        IQueryable<Subcategory> Query();
        void Delete(Subcategory subcategory);
        void DeleteRange(List<Subcategory> subcategories);
        Task<List<Subcategory>> GetByIdsAsync(List<Guid> ids);
        Task<bool> HasSubcategoriesAsync(Guid categoryId);
        Task<bool> HasSubcategoriesAsync(List<Guid> categoryIds);

        Task<(int successCount, int updateCount, List<string> errors)> UploadSubcategoriesAsync(Microsoft.AspNetCore.Http.IFormFile file, Guid companyId);
        Task<bool> ExistsByNameAsync(string name, Guid companyId, Guid? excludeId = null);
    }
}
