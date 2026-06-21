using Company.Application.Common.Interfaces;
using Company.Application.Common.Models;
using Company.Domain.Entities;
using Company.Infrastructure.Persistence;
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

            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE IdentityDb.dbo.Subscriptions SET CompanyName = {0} WHERE CompanyId = {1}", 
                    profile.Name, profile.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB-SYNC-ERROR] Failed to sync CompanyName to IdentityDb: {ex.Message}");
            }

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
                .IgnoreQueryFilters()
                .Include(c => c.Addresses)
                .Include(c => c.BankDetails)
                .Include(c => c.AuthorizedSignatories) // Signatories load karein
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<CompanyProfile?> GetByNameAsync(string name)
        {
            return await _context.CompanyProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
        }

        public async Task<CompanyProfile?> GetByEmailAsync(string email)
        {
            return await _context.CompanyProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => 
                    (c.PrimaryEmail != null && c.PrimaryEmail.ToLower() == email.ToLower()) || 
                    (c.Email != null && c.Email.ToLower() == email.ToLower()));
        }

        public async Task<CompanyProfile?> GetByPhoneAsync(string phone)
        {
            return await _context.CompanyProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.PrimaryPhone != null && c.PrimaryPhone == phone);
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
            var companyId = _currentUserService.CompanyId;
            var query = _context.CompanyProfiles
                .Include(c => c.Addresses)
                .Include(c => c.BankDetails)
                .AsQueryable();

            // 🚀 TENANT ISOLATION: Only Platform Admin sees all companies.
            // Standard Tenant Admins see only their own company profile.
            if (!_currentUserService.IsPlatformAdmin)
            {
                if (companyId != null && companyId != Guid.Empty)
                {
                    query = query.Where(c => c.Id == companyId);
                }
                else
                {
                    // If somehow no companyId is present for a non-platform admin, return nothing
                    query = query.Where(c => false);
                }
            }

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

        public async Task<bool> HasDuplicateBankAccountAsync(string accountNumber, string ifscCode, Guid? excludeCompanyId)
        {
            if (string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(ifscCode))
            {
                return false;
            }

            var query = _context.BankDetails.AsNoTracking().AsQueryable();

            if (excludeCompanyId.HasValue && excludeCompanyId.Value != Guid.Empty)
            {
                query = query.Where(b => b.CompanyProfileId != excludeCompanyId.Value);
            }

            return await query.AnyAsync(b => b.AccountNumber == accountNumber && b.IfscCode != null && b.IfscCode.ToUpper() == ifscCode.ToUpper());
        }
    }
}
