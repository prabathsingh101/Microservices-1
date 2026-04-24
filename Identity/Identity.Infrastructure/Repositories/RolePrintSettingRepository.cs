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

    public async Task<IEnumerable<RolePrintSetting>> GetPrintSettingsByRoleIdAsync(Guid roleId, Guid? companyId)
    {
        return await _context.RolePrintSettings
            .Where(rs => rs.RoleId == roleId && rs.CompanyId == companyId)
            .ToListAsync();
    }

    public async Task UpdateRolePrintSettingsAsync(Guid roleId, IEnumerable<RolePrintSetting> settings, Guid? companyId)
    {
        var existingSettings = await _context.RolePrintSettings
            .Where(rs => rs.RoleId == roleId && rs.CompanyId == companyId)
            .ToListAsync();

        foreach (var setting in settings)
        {
            var existing = existingSettings.FirstOrDefault(x => x.PageName == setting.PageName);
            if (existing != null)
            {
                existing.UpdateFormat(setting.PrintFormat);
            }
            else
            {
                setting.RoleId = roleId; 
                setting.CompanyId = companyId; // Explicitly set CompanyId
                await _context.RolePrintSettings.AddAsync(setting);
            }
        }

        await _context.SaveChangesAsync();
    }
}
