using Identity.Domain.PrintSettings;

namespace Identity.Application.Interfaces;

public interface IRolePrintSettingRepository
{
    Task<IEnumerable<RolePrintSetting>> GetPrintSettingsByRoleIdAsync(Guid roleId, Guid? companyId);
    Task UpdateRolePrintSettingsAsync(Guid roleId, IEnumerable<RolePrintSetting> settings, Guid? companyId);
}
