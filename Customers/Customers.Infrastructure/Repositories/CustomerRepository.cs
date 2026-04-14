using Customers.Application.Common.Interfaces;
using Customers.Application.DTOs;
using Customers.Domain.Entities;
using Customers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Customers.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly CustomerDbContext _context;

        public CustomerRepository(CustomerDbContext context)
        {
            _context = context;
        }

        public IQueryable<Customer> Query() => _context.Customers.AsNoTracking();

        public async Task AddAsync(Customer customer)
        {
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Customer>> GetAllAsync()
            => await _context.Customers.ToListAsync();

        public async Task<Customer?> GetByIdAsync(Guid id)
            => await _context.Customers.FindAsync(id);

        public async Task UpdateAsync(Customer customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Customer customer)
        {
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
        }

        public async Task<Dictionary<Guid, string>> GetCustomerNamesByIdsAsync(List<Guid> ids)
        {
            if (ids == null || !ids.Any())
                return new Dictionary<Guid, string>();

            var distinctIds = ids.Distinct().ToList();

            return await _context.Customers
                .AsNoTracking() 
                .Where(c => distinctIds.Contains(c.Id))
                .Select(c => new { c.Id, c.CustomerName }) 
                .ToDictionaryAsync(x => x.Id, x => x.CustomerName ?? string.Empty);
        }

        public async Task<string?> GetCustomerNameByIdAsync(Guid id)
        {
            return await _context.Customers
                .Where(c => c.Id == id)
                .Select(c => c.CustomerName)
                .FirstOrDefaultAsync();
        }

        public async Task<List<CustomerLookupDto>> GetCustomersLookupAsync()
        {
            return await _context.Customers
                .AsNoTracking()
                .Select(c => new CustomerLookupDto
                {
                    Id = c.Id,
                    Name = c.CustomerName ?? string.Empty
                })
                .ToListAsync();
        }

        public async Task<List<Guid>> GetIdsByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return new List<Guid>();

            return await _context.Customers
                .AsNoTracking()
                .Where(c => c.CustomerName != null && EF.Functions.Like(c.CustomerName, $"%{name}%"))
                .Select(c => c.Id)
                .ToListAsync();
        }
    }
}
