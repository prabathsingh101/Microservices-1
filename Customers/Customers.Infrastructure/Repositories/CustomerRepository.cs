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
        private readonly ICurrentUserService _currentUserService;

        public CustomerRepository(CustomerDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        private Guid _companyId => _currentUserService.CompanyId ?? Guid.Empty;
        private string? _branchId => _currentUserService.BranchId;

        public IQueryable<Customer> Query() => _context.Customers.AsNoTracking()
            .Where(x => x.CompanyId == _companyId);

        public async Task AddAsync(Customer customer)
        {
            customer.CompanyId = _companyId;
            if (string.IsNullOrEmpty(customer.BranchId))
            {
                customer.BranchId = _branchId;
            }
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Customer>> GetAllAsync()
            => await Query().ToListAsync();

        public async Task<Customer?> GetByIdAsync(Guid id)
            => await Query().FirstOrDefaultAsync(x => x.Id == id);

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

            return await _context.Customers.AsNoTracking()
                .Where(c => c.CompanyId == _companyId && distinctIds.Contains(c.Id))
                .Select(c => new { c.Id, c.CustomerName }) 
                .ToDictionaryAsync(x => x.Id, x => x.CustomerName ?? string.Empty);
        }

        public async Task<Dictionary<Guid, CustomerDto>> GetCustomerDetailsByIdsAsync(List<Guid> ids)
        {
            if (ids == null || !ids.Any())
                return new Dictionary<Guid, CustomerDto>();

            var distinctIds = ids.Distinct().ToList();

            return await _context.Customers.AsNoTracking()
                .Where(c => c.CompanyId == _companyId && distinctIds.Contains(c.Id))
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    CustomerName = c.CustomerName,
                    Phone = c.Phone,
                    Email = c.Email,
                    GstNumber = c.GstNumber,
                    BillingAddressLine = c.BillingAddress != null ? c.BillingAddress.AddressLine : null,
                    ShippingAddressLine = c.ShippingAddress != null ? c.ShippingAddress.AddressLine : null
                })
                .ToDictionaryAsync(x => x.Id, x => x);
        }

        public async Task<string?> GetCustomerNameByIdAsync(Guid id)
        {
            return await _context.Customers.AsNoTracking()
                .Where(c => c.CompanyId == _companyId && c.Id == id)
                .Select(c => c.CustomerName)
                .FirstOrDefaultAsync();
        }

        public async Task<List<CustomerLookupDto>> GetCustomersLookupAsync()
        {
            return await Query()
                .Select(c => new CustomerLookupDto
                {
                    Id = c.Id,
                    Name = c.CustomerName ?? string.Empty,
                    Phone = c.Phone
                })
                .ToListAsync();
        }

        public async Task<List<Guid>> GetIdsByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return new List<Guid>();

            return await Query()
                .Where(c => c.CustomerName != null && EF.Functions.Like(c.CustomerName, $"%{name}%"))
                .Select(c => c.Id)
                .ToListAsync();
        }
    }
}
