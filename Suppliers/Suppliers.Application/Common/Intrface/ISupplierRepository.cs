using Suppliers.Application.DTOs;
using Suppliers.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Suppliers.Application.Common.Interfaces
{
    public interface ISupplierRepository
    {
        IQueryable<Supplier> Query();
        Task<Supplier?> GetByIdAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<IEnumerable<Supplier>> GetAllAsync();
        Task AddAsync(Supplier supplier);
        Task UpdateAsync(Supplier supplier);
        Task SaveChangesAsync();
        Task<List<SupplierSelectDto>> GetSuppliersByIdsAsync(List<Guid> ids);
        Task<List<Guid>> GetIdsByNameAsync(string name);
    }
}
