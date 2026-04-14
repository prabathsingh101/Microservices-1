using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    private readonly Identity.Infrastructure.Persistence.IdentityDbContext _context;

    public RolesController(
        IRoleRepository roleRepository, 
        IRolePermissionRepository permissionRepository,
        Identity.Infrastructure.Persistence.IdentityDbContext context)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        
        if (Guid.TryParse(companyIdClaim, out var companyId))
        {
            var roles = await _roleRepository.GetByCompanyAsync(companyId);
            return Ok(roles);
        }

        // Fallback for Super Admin or users without company attribution
        var allRoles = await _roleRepository.GetAllAsync();
        return Ok(allRoles);
    }

    [HttpGet("{roleId}/permissions")]
    public async Task<IActionResult> GetPermissions(Guid roleId)
    {
        var perms = await _permissionRepository.GetPermissionsByRoleIdAsync(roleId);
        return Ok(perms);
    }

    [HttpPut("{roleId}/permissions")]
    public async Task<IActionResult> UpdatePermissions(Guid roleId, [FromBody] IEnumerable<RolePermission> permissions)
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        if (!Guid.TryParse(companyIdClaim, out var companyId))
        {
            return BadRequest("CompanyId not found in token");
        }

        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null) return NotFound();

        Guid targetRoleId = roleId;

        // If it's a System Role (CompanyId is null), we must CLONE it for this company
        if (role.CompanyId == null)
        {
            // Check if a customized role already exists for this company with this name
            var existingCustom = await _context.Roles
                                .FirstOrDefaultAsync(r => r.RoleName == role.RoleName && r.CompanyId == companyId);

            if (existingCustom == null)
            {
                // Start Transaction for complex update
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Create new Custom Role
                    var newRole = new Role(role.RoleName, companyId);
                    await _context.Roles.AddAsync(newRole);
                    await _context.SaveChangesAsync();
                    targetRoleId = newRole.Id;

                    // 2. Map Permissions (Frontend usually sends the modified ones)
                    // We link them to the NEW role
                    foreach (var p in permissions)
                    {
                        var np = new RolePermission(targetRoleId, p.MenuId, p.CanView, p.CanAdd, p.CanEdit, p.CanDelete, p.AdditionalActions);
                        await _context.RolePermissions.AddAsync(np);
                    }

                    // 3. IMPORTANT: Update existing users of this company from Global Role to Custom Role
                    var usersToUpdate = await _context.UserRoles
                        .Include(ur => ur.User)
                        .Where(ur => ur.RoleId == roleId && ur.User.CompanyId == companyId)
                        .ToListAsync();

                    foreach (var ur in usersToUpdate)
                    {
                        // We need to re-assign the role. Since Id is Guid PK, we might need a better way if mapped differently
                        // But essentially we change the RoleId
                         _context.UserRoles.Remove(ur);
                         await _context.UserRoles.AddAsync(new Domain.Users.UserRole(ur.UserId, targetRoleId));
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Ok(new { Message = "Role customized for company and users reassigned", NewRoleId = targetRoleId });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, "Failed to customize role");
                }
            }
            else
            {
                targetRoleId = existingCustom.Id;
            }
        }

        await _permissionRepository.UpdateRolePermissionsAsync(targetRoleId, permissions);
        return Ok();
    }

    [HttpGet("{roleId}/print-settings")]
    public async Task<IActionResult> GetPrintSettings(Guid roleId, [FromServices] IRolePrintSettingRepository printRepo)
    {
        var settings = await printRepo.GetPrintSettingsByRoleIdAsync(roleId);
        return Ok(settings);
    }

    [HttpPut("{roleId}/print-settings")]
    public async Task<IActionResult> UpdatePrintSettings(Guid roleId, [FromBody] IEnumerable<Domain.PrintSettings.RolePrintSetting> settings, [FromServices] IRolePrintSettingRepository printRepo)
    {
        await printRepo.UpdateRolePrintSettingsAsync(roleId, settings);
        return Ok();
    }
}
