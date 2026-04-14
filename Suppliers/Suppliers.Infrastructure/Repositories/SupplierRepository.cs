using Suppliers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Suppliers.Application.Common.Interfaces;
using Suppliers.Application.DTOs;
using Suppliers.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Suppliers.Infrastructure.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly SupplierDbContext _context;

        public SupplierRepository(SupplierDbContext context)
        {
            _context = context;
        }

        public IQueryable<Supplier> Query() => _context.Suppliers.AsNoTracking();

        public async Task<IEnumerable<Supplier>> GetAllAsync() =>
            await _context.Suppliers.AsNoTracking().ToListAsync();

        public async Task<Supplier?> GetByIdAsync(Guid id)
        {
            return await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task AddAsync(Supplier supplier)
        {
            await _context.Suppliers.AddAsync(supplier);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Supplier supplier)
        {
            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();
        }
        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Suppliers.AnyAsync(s => s.Id == id);
        }
        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task<List<SupplierSelectDto>> GetSuppliersByIdsAsync(List<Guid> ids)
        {
            if (ids == null || !ids.Any()) return new List<SupplierSelectDto>();

            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .Where(s => ids.Contains(s.Id))
                .OrderBy(s => s.Name)
                .Select(s => new SupplierSelectDto
                {
                    Id = s.Id,
                    Name = s.Name
                })
                .ToListAsync();

            return suppliers ?? new List<SupplierSelectDto>();
        }

        public async Task<List<Guid>> GetIdsByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return new List<Guid>();

            var s = name.ToLower().Trim();

            return await _context.Suppliers
                .AsNoTracking()
                .Where(s_ent => s_ent.Name.ToLower().Contains(s))
                .Select(s_ent => s_ent.Id)
                .ToListAsync();
        }
    }
}
