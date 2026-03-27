using Identity.Domain.PrintSettings;

namespace Identity.Application.Interfaces;

public interface IRolePrintSettingRepository
{
    Task<IEnumerable<RolePrintSetting>> GetPrintSettingsByRoleIdAsync(int roleId);
    Task UpdateRolePrintSettingsAsync(int roleId, IEnumerable<RolePrintSetting> settings);
}
