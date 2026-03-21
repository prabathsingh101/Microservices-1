using Customers.Application.Common.Interfaces;
using Customers.Domain.Entities;
using Customers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

        public async Task<Customer?> GetByIdAsync(int id)
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

        public async Task<Dictionary<int, string>> GetCustomerNamesByIdsAsync(List<int> ids)
        {
            if (ids == null || !ids.Any())
                return new Dictionary<int, string>();

            var distinctIds = ids.Distinct().ToList();

            return await _context.Customers
                .AsNoTracking() 
                .Where(c => distinctIds.Contains(c.Id))
                .Select(c => new { c.Id, c.CustomerName }) 
                .ToDictionaryAsync(x => x.Id, x => x.CustomerName);
        }

        public async Task<string?> GetCustomerNameByIdAsync(int id)
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
                    Name = c.CustomerName 
                })
                .ToListAsync();
        }

        public async Task<List<int>> GetIdsByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return new List<int>();

            return await _context.Customers
                .AsNoTracking()
                .Where(c => EF.Functions.Like(c.CustomerName, $"%{name}%"))
                .Select(c => c.Id)
                .ToListAsync();
        }
    }
}
