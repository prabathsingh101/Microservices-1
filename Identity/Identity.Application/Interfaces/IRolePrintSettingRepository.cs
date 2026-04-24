using Identity.Domain.PrintSettings;

namespace Identity.Application.Interfaces;

public interface IRolePrintSettingRepository
{
    Task<IEnumerable<RolePrintSetting>> GetPrintSettingsByRoleIdAsync(Guid roleId, Guid? companyId, string? branchId);
    Task UpdateRolePrintSettingsAsync(Guid roleId, IEnumerable<RolePrintSetting> settings, Guid? companyId, string? branchId);
}
