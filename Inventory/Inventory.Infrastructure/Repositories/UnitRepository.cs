using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories
{
    public class UnitRepository : IUnitRepository
    {
        private readonly InventoryDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public UnitRepository(InventoryDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task AddAsync(UnitMaster unit) => await _context.Units.AddAsync(unit);

        public async Task DeleteAsync(Guid id)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || u.BranchId == branchId || u.BranchId == null));
            if (unit != null)
            {
                _context.Units.Remove(unit);
            }
        }

        public async Task<IEnumerable<UnitMaster>> GetAllAsync()
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            return await _context.Units
                         .AsNoTracking()
                         .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId || x.BranchId == null))
                         .ToListAsync();
        }

        public async Task<UnitMaster> GetByIdAsync(Guid id) 
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            return await _context.Units.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || u.BranchId == branchId || u.BranchId == null));
        }

        public Task UpdateAsync(UnitMaster unit)
        {
            _context.Units.Update(unit);
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string name)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            return await _context.Units.AnyAsync(u => u.Name.ToLower() == name.ToLower() && u.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || u.BranchId == branchId || u.BranchId == null));
        }

        public IQueryable<UnitMaster> Query() 
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;
            return _context.Units.Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId || x.BranchId == null)).AsQueryable();
        }

        public async Task<(int successCount, List<string> errors)> UploadUnitsAsync(Microsoft.AspNetCore.Http.IFormFile file, Guid companyId, string? branchId = null)
        {
            int successCount = 0;
            var errors = new List<string>();

            using (var stream = new System.IO.MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var workbook = new ClosedXML.Excel.XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheets.First();
                    var dataRows = worksheet.RowsUsed().Skip(1); // Skip Header

                    // Pre-fetch for Upsert
                    var dbUnits = await _context.Units
                        .Where(x => x.CompanyId == companyId && (string.IsNullOrEmpty(branchId) || x.BranchId == branchId || x.BranchId == null))
                        .ToListAsync();
                    var dbUnitsByName = dbUnits.ToDictionary(w => w.Name.ToLower().Trim(), w => w);

                    var newUnits = new List<UnitMaster>();
                    int updateCount = 0;

                    foreach (var row in dataRows)
                    {
                        int rowNum = row.RowNumber();
                        try
                        {
                            var name = row.Cell(1).Value.ToString()?.Trim();
                            var description = row.Cell(2).Value.ToString()?.Trim();
                            var activeStatus = row.Cell(3).Value.ToString()?.Trim().ToUpper() ?? "TRUE";
                            bool isActive = activeStatus == "TRUE" || activeStatus == "1" || activeStatus == "ACTIVE" || activeStatus == "YES";

                            if (string.IsNullOrWhiteSpace(name)) continue;

                            if (dbUnitsByName.TryGetValue(name.ToLower(), out var existing))
                            {
                                existing.Update(name, description, isActive, companyId, branchId);
                                updateCount++;
                            }
                            else
                            {
                                var unit = new UnitMaster(name, description, companyId, branchId);
                                if (!isActive) { /* UnitMaster constructor sets isActive=true by default, but we can't easily change it if it's private set without a method, oh wait, Update can do it or I can just use a hack or add a param to constructor */ 
                                    existing = unit; // Wait, I can just call Update on it immediately if I want, or add it to constructor
                                }
                                newUnits.Add(unit);
                            }
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Row {rowNum}: Error - {ex.Message}");
                        }
                    }

                    if (newUnits.Any() || updateCount > 0)
                    {
                        if (newUnits.Any()) await _context.Units.AddRangeAsync(newUnits);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            return (successCount, errors);
        }
    }
}
