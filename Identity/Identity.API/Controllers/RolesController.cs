using Microsoft.AspNetCore.Mvc;
using Identity.Application.Interfaces;
using Identity.Domain.Permissions;
using Identity.Domain.Roles;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IRolePermissionRepository _permissionRepository;

    public RolesController(IRoleRepository roleRepository, IRolePermissionRepository permissionRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleRepository.GetAllAsync();
        return Ok(roles);
    }

    [HttpGet("{roleId}/permissions")]
    public async Task<IActionResult> GetPermissions(int roleId)
    {
        var perms = await _permissionRepository.GetPermissionsByRoleIdAsync(roleId);
        return Ok(perms);
    }

    [HttpPut("{roleId}/permissions")]
    public async Task<IActionResult> UpdatePermissions(int roleId, [FromBody] IEnumerable<RolePermission> permissions)
    {
        await _permissionRepository.UpdateRolePermissionsAsync(roleId, permissions);
        return Ok();
    }

    [HttpGet("{roleId}/print-settings")]
    public async Task<IActionResult> GetPrintSettings(int roleId, [FromServices] IRolePrintSettingRepository printRepo)
    {
        var settings = await printRepo.GetPrintSettingsByRoleIdAsync(roleId);
        return Ok(settings);
    }

    [HttpPut("{roleId}/print-settings")]
    public async Task<IActionResult> UpdatePrintSettings(int roleId, [FromBody] IEnumerable<Domain.PrintSettings.RolePrintSetting> settings, [FromServices] IRolePrintSettingRepository printRepo)
    {
        await printRepo.UpdateRolePrintSettingsAsync(roleId, settings);
        return Ok();
    }
}
