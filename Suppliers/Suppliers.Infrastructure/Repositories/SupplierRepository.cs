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
        private readonly ICurrentUserService _currentUserService;

        public SupplierRepository(SupplierDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        private Guid _companyId => _currentUserService.CompanyId ?? Guid.Empty;

        public IQueryable<Supplier> Query() => _context.Suppliers.AsNoTracking().Where(x => x.CompanyId == _companyId);

        public async Task<IEnumerable<Supplier>> GetAllAsync() =>
            await _context.Suppliers.AsNoTracking().Where(x => x.CompanyId == _companyId).ToListAsync();

        public async Task<Supplier?> GetByIdAsync(Guid id)
        {
            return await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && s.CompanyId == _companyId);
        }

        public async Task AddAsync(Supplier supplier)
        {
            supplier.CompanyId = _companyId;
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
            return await _context.Suppliers.AnyAsync(s => s.Id == id && s.CompanyId == _companyId);
        }
        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task<List<SupplierSelectDto>> GetSuppliersByIdsAsync(List<Guid> ids)
        {
            if (ids == null || !ids.Any()) return new List<SupplierSelectDto>();

            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .Where(s => ids.Contains(s.Id) && s.CompanyId == _companyId)
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
                .Where(s_ent => s_ent.Name.ToLower().Contains(s) && s_ent.CompanyId == _companyId)
                .Select(s_ent => s_ent.Id)
                .ToListAsync();
        }
    }
}
