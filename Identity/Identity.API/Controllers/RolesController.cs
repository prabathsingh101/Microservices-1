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
        var subscriptions = await _context.Subscriptions.ToDictionaryAsync(s => s.CompanyId, s => s.CompanyName);

        var allRoles = await _roleRepository.GetAllAsync();
        var result = allRoles.Select(r => new {
            r.Id,
            r.RoleName,
            r.CompanyId,
            r.BranchId,
            r.CreatedBy,
            r.CreatedDate,
            r.LastModifiedBy,
            r.LastModifiedDate,
            r.Description, // 🔥 Return description
            CompanyName = r.CompanyId.HasValue && subscriptions.ContainsKey(r.CompanyId.Value) 
                          ? subscriptions[r.CompanyId.Value] 
                          : (r.CompanyId == null ? "System" : "Unknown")
        });

        return Ok(result);
    }

    [HttpGet("company/{companyId}")]
    public async Task<IActionResult> GetRolesByCompanyId(string companyId)
    {
        var subscriptions = await _context.Subscriptions.ToDictionaryAsync(s => s.CompanyId, s => s.CompanyName);

        // 🎯 Case 1: Master/System Context (No CompanyId)
        if (string.IsNullOrEmpty(companyId) || companyId.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            var systemRoles = await _context.Roles
                .Where(r => r.CompanyId == null)
                .AsNoTracking()
                .ToListAsync();
            
            var res = systemRoles.Select(r => new { r.Id, r.RoleName, r.CompanyId, CompanyName = "System" });
            return Ok(res);
        }

        // 🎯 Case 2: Tenant Context (Specific CompanyId)
        if (Guid.TryParse(companyId, out var cid))
        {
            // 🚀 ROBUST CHECK: Is the logged-in user a Global Root Admin?
            var userRoles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
            bool isGlobalRoot = userRoles.Contains("Default Admin");

            // 🏗️ Fetch roles
            // If Global Admin, they see (Selected Company Roles + System Roles)
            // If Tenant Admin, they see (Only their Company Roles)
            var roles = await _context.Roles
                .Where(r => r.CompanyId == cid || (isGlobalRoot && r.CompanyId == null))
                .AsNoTracking()
                .ToListAsync();

            var result = roles.Select(r => new { 
                r.Id, 
                r.RoleName, 
                r.CompanyId, 
                r.BranchId,
                r.CreatedBy,
                r.CreatedDate,
                r.LastModifiedBy,
                r.LastModifiedDate,
                r.Description,
                CompanyName = r.CompanyId == null ? "System" : (subscriptions.ContainsKey(cid) ? subscriptions[cid] : "Unknown")
            }).ToList();

            return Ok(result);
        }

        return BadRequest("Invalid CompanyId format");
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
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null) return NotFound();

        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        Guid? loggedInCompanyId = null;
        if (Guid.TryParse(companyIdClaim, out var cid))
        {
            loggedInCompanyId = cid;
        }

        Guid targetRoleId = roleId;

        // 🚀 SUPER ADMIN BYPASS: Allow global admins to edit any role across companies
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        bool isSuperAdmin = roles.Any(r => r.Contains("Admin", StringComparison.OrdinalIgnoreCase));

        // 🧠 LOGIC: If user is System Admin or Super Admin, they can edit anything directly.
        // If user is a Tenant Admin (with CompanyId and NOT a SuperAdmin), they can only edit their own or CLONE a system role.
        if (loggedInCompanyId.HasValue && !isSuperAdmin)
        {
            if (role.CompanyId == null)
            {
                // 🏗️ CLONING: Tenant Admin trying to customize a System Role for their company
                var existingCustom = await _context.Roles
                                    .FirstOrDefaultAsync(r => r.RoleName == role.RoleName && r.CompanyId == loggedInCompanyId);

                if (existingCustom == null)
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var newRole = new Role(role.RoleName, loggedInCompanyId.Value);
                        await _context.Roles.AddAsync(newRole);
                        await _context.SaveChangesAsync();
                        targetRoleId = newRole.Id;

                        foreach (var p in permissions)
                        {
                            var np = new RolePermission(targetRoleId, p.MenuId, p.CanView, p.CanAdd, p.CanEdit, p.CanDelete, p.AdditionalActions, loggedInCompanyId.Value);
                            await _context.RolePermissions.AddAsync(np);
                        }

                        var usersToUpdate = await _context.UserRoles
                            .Include(ur => ur.User)
                            .Where(ur => ur.RoleId == roleId && ur.User.CompanyId == loggedInCompanyId)
                            .ToListAsync();

                        foreach (var ur in usersToUpdate)
                        {
                             _context.UserRoles.Remove(ur);
                             await _context.UserRoles.AddAsync(new Domain.Users.UserRole(ur.UserId, targetRoleId, loggedInCompanyId.Value, ur.BranchId));
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return Ok(new { Message = "Role customized for company", NewRoleId = targetRoleId });
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
            else if (role.CompanyId != loggedInCompanyId)
            {
                // 🛡️ SECURITY: Tenant Admin trying to edit ANOTHER company's role
                return Forbid();
            }
        }
        else
        {
            // 🚀 SUPER ADMIN: Just ensure the RolePermission entries have the Role's CompanyId and the current context's BranchId
            var currentBranchId = Request.Headers["X-Branch-Id"].ToString();
            if (string.IsNullOrEmpty(currentBranchId)) currentBranchId = User.FindFirst("BranchId")?.Value;

            foreach (var p in permissions)
            {
                p.CompanyId = role.CompanyId;
                // If p.BranchId is null or empty, it means Global was selected. 
                // We only fallback to the header branch if we are NOT a super admin doing global management.
            }
        }

        await _permissionRepository.UpdateRolePermissionsAsync(targetRoleId, permissions);
        return Ok();
    }

    [HttpGet("{roleId}/print-settings")]
    public async Task<IActionResult> GetPrintSettings(Guid roleId, [FromQuery] Guid? companyId, [FromQuery] string? branchId, [FromServices] IRolePrintSettingRepository printRepo)
    {
        var targetCompanyId = companyId;
        var targetBranchId = branchId;

        if (targetCompanyId == null)
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (Guid.TryParse(companyIdClaim, out var cid)) targetCompanyId = cid;
        }

        if (string.IsNullOrEmpty(targetBranchId))
        {
            targetBranchId = User.FindFirst("BranchId")?.Value;
        }

        var settings = await printRepo.GetPrintSettingsByRoleIdAsync(roleId, targetCompanyId, targetBranchId);
        return Ok(settings);
    }

    [HttpPut("{roleId}/print-settings")]
    public async Task<IActionResult> UpdatePrintSettings(Guid roleId, [FromBody] IEnumerable<Domain.PrintSettings.RolePrintSetting> settings, [FromQuery] Guid? companyId, [FromQuery] string? branchId, [FromServices] IRolePrintSettingRepository printRepo)
    {
        var targetCompanyId = companyId;
        var targetBranchId = branchId;

        if (targetCompanyId == null)
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (Guid.TryParse(companyIdClaim, out var cid)) targetCompanyId = cid;
        }

        if (string.IsNullOrEmpty(targetBranchId))
        {
            targetBranchId = User.FindFirst("BranchId")?.Value;
        }

        await printRepo.UpdateRolePrintSettingsAsync(roleId, settings, targetCompanyId, targetBranchId);
        return Ok();
    }

    // --- Role Management CRUD ---

    public record RoleRequest(string RoleName, Guid? CompanyId = null, string? BranchId = null, string? Description = null);

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] RoleRequest request)
    {
        Guid? targetCompanyId = request.CompanyId;
        string? targetBranchId = request.BranchId;

        // 🛡️ FIX: If it's a global root admin and they sent NULL, keep it NULL (System Role)
        bool isGlobalRoot = User.IsInRole("Default Admin");

        if (targetCompanyId == null && !isGlobalRoot)
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (Guid.TryParse(companyIdClaim, out var companyId))
            {
                targetCompanyId = companyId;
            }
        }

        if ((string.IsNullOrWhiteSpace(targetBranchId) || targetBranchId == "GLOBAL") && !isGlobalRoot)
        {
            var headerBranch = Request.Headers["X-Branch-Id"].ToString();
            targetBranchId = !string.IsNullOrWhiteSpace(headerBranch) 
                ? headerBranch 
                : User.FindFirst("BranchId")?.Value;
        }

        // Final normalization
        if (string.IsNullOrWhiteSpace(targetBranchId) || targetBranchId == "GLOBAL") targetBranchId = null;

        var role = new Role(request.RoleName, targetCompanyId, targetBranchId, request.Description);
        await _roleRepository.AddAsync(role);
        await _context.SaveChangesAsync();

        return Ok(role);
    }

    [HttpPut("{roleId}")]
    public async Task<IActionResult> UpdateRoleName(Guid roleId, [FromBody] RoleRequest request)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null) return NotFound();

        role.RoleName = request.RoleName;
        role.Description = request.Description; // 🔥 Update description
        // 🛡️ Update BranchId: Map empty or 'GLOBAL' back to null
        role.BranchId = (string.IsNullOrWhiteSpace(request.BranchId) || request.BranchId == "GLOBAL") ? null : request.BranchId;

        await _roleRepository.UpdateAsync(role);
        await _context.SaveChangesAsync();

        return Ok(role);
    }

    [HttpDelete("{roleId}")]
    public async Task<IActionResult> DeleteRole(Guid roleId)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null) return NotFound();

        // Optional: Check if it's a system role
        if (role.CompanyId == null)
        {
            return BadRequest("System roles cannot be deleted");
        }

        await _roleRepository.DeleteAsync(roleId);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Role deleted successfully" });
    }
}
