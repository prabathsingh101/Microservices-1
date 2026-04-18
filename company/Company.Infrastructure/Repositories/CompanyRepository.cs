using Company.Application.Common.Interfaces;
using Company.Application.Common.Models;
using Company.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Company.Infrastructure.Repositories
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly CompanyDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CompanyRepository(CompanyDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        // --- CREATE ---
        public async Task<Guid> InsertCompanyAsync(CompanyProfile company)
        {
            _context.CompanyProfiles.Add(company);
            await _context.SaveChangesAsync();
            return company.Id;
        }

        // --- UPDATE ---
        public async Task<Guid> UpsertCompanyProfileAsync(CompanyProfile profile)
        {
            _context.CompanyProfiles.Update(profile);
            await _context.SaveChangesAsync();
            return profile.Id;
        }

        // --- READ: GET MASTER PROFILE (Optimized) ---
        public async Task<CompanyProfile?> GetCompanyProfileAsync()
        {
            var companyId = _currentUserService.CompanyId;
            
            return await _context.CompanyProfiles
                .Include(c => c.Addresses)
                .Include(c => c.BankDetails)
                .Include(c => c.AuthorizedSignatories)
                .FirstOrDefaultAsync(c => c.Id == companyId);
        }

        // --- READ: GET BY ID ---
        public async Task<CompanyProfile?> GetByIdAsync(Guid id)
        {
            return await _context.CompanyProfiles
                .Include(c => c.Addresses)
                .Include(c => c.BankDetails)
                .Include(c => c.AuthorizedSignatories) // Signatories load karein
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<CompanyProfile?> GetByNameAsync(string name)
        {
            return await _context.CompanyProfiles
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
        }


        // --- DELETE ---
        public async Task<bool> DeleteCompanyProfileAsync(Guid id)
        {
            // Record search kar rahe hain delete karne se pehle
            var company = await _context.CompanyProfiles.FindAsync(id);

            if (company == null) return false;

            // Profile remove karenge, cascade delete baqi records handle kar lega
            _context.CompanyProfiles.Remove(company);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<GridResponse<CompanyProfile>> GetPagedAsync(GridRequest request)
        {
            var query = _context.CompanyProfiles
                .Include(c => c.Addresses)
                .Include(c => c.BankDetails)
                .AsQueryable();

            // Search Filter
            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(c => c.Name.Contains(request.Search) || c.Gstin.Contains(request.Search));
            }

            // Total Count
            var total = await query.CountAsync();

            // Sorting (Simplistic)
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                if (request.SortDirection == "desc")
                    query = query.OrderByDescending(c => EF.Property<object>(c, request.SortBy));
                else
                    query = query.OrderBy(c => EF.Property<object>(c, request.SortBy));
            }
            else
            {
                query = query.OrderBy(c => c.Name);
            }

            // Counts before paging but after filtering
            var activeCount = await query.CountAsync(c => c.IsActive);
            var inactiveCount = await query.CountAsync(c => !c.IsActive);

            // Pagination
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new GridResponse<CompanyProfile> 
            { 
                Items = items, 
                TotalCount = total,
                ActiveCount = activeCount,
                InactiveCount = inactiveCount
            };
        }
    }
}
