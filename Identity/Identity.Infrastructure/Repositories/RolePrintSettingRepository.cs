using Identity.Application.Interfaces;
using Identity.Domain.PrintSettings;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class RolePrintSettingRepository : IRolePrintSettingRepository
{
    private readonly IdentityDbContext _context;

    public RolePrintSettingRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RolePrintSetting>> GetPrintSettingsByRoleIdAsync(Guid roleId, Guid? companyId, string? branchId)
    {
        return await _context.RolePrintSettings
            .Where(rs => rs.RoleId == roleId && rs.CompanyId == companyId && rs.BranchId == branchId)
            .ToListAsync();
    }

    public async Task UpdateRolePrintSettingsAsync(Guid roleId, IEnumerable<RolePrintSetting> settings, Guid? companyId, string? branchId)
    {
        var existingSettings = await _context.RolePrintSettings
            .Where(rs => rs.RoleId == roleId && rs.CompanyId == companyId && rs.BranchId == branchId)
            .ToListAsync();

        foreach (var setting in settings)
        {
            var existing = existingSettings.FirstOrDefault(x => x.PageName == setting.PageName);
            if (existing != null)
            {
                existing.UpdateFormat(setting.PrintFormat);
                _context.Entry(existing).State = EntityState.Modified; // Force auditing trigger
            }
            else
            {
                setting.RoleId = roleId; 
                setting.CompanyId = companyId;
                setting.BranchId = branchId;
                await _context.RolePrintSettings.AddAsync(setting);
            }
        }

        await _context.SaveChangesAsync();
    }
}
