using Identity.Domain.PrintSettings;

namespace Identity.Application.Interfaces;

public interface IRolePrintSettingRepository
{
    Task<IEnumerable<RolePrintSetting>> GetPrintSettingsByRoleIdAsync(Guid roleId);
    Task UpdateRolePrintSettingsAsync(Guid roleId, IEnumerable<RolePrintSetting> settings);
}
