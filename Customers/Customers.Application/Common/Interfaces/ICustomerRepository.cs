using Customers.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Customers.Application.DTOs;
namespace Customers.Application.Common.Interfaces
{
    public interface ICustomerRepository
    {
        IQueryable<Customer> Query();
        Task AddAsync(Customer customer);
        Task<List<Customer>> GetAllAsync();
        Task<Customer?> GetByIdAsync(Guid id);
        Task UpdateAsync(Customer customer);
        Task DeleteAsync(Customer customer);

        //bulk customer call
        Task<Dictionary<Guid, string>> GetCustomerNamesByIdsAsync(List<Guid> ids);

        //single cusomer call
        Task<string?> GetCustomerNameByIdAsync(Guid id);

        Task<List<CustomerLookupDto>> GetCustomersLookupAsync();

        Task<List<Guid>> GetIdsByNameAsync(string name);
    }
}
