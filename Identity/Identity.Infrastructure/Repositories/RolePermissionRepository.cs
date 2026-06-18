using Identity.Application.Interfaces;
using Identity.Domain.Permissions;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly IdentityDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RolePermissionRepository(IdentityDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<RolePermission>> GetPermissionsByRoleIdAsync(Guid roleId, Guid? userId = null)
    {
        var userPermissions = await _context.RolePermissions
            .IgnoreQueryFilters()
            .Where(rp => rp.RoleId == roleId && rp.UserId == userId)
            .ToListAsync();

        if (userId.HasValue && !userPermissions.Any())
        {
            return await _context.RolePermissions
                .IgnoreQueryFilters()
                .Where(rp => rp.RoleId == roleId && rp.UserId == null)
                .ToListAsync();
        }

        return userPermissions;
    }

    public async Task UpdateRolePermissionsAsync(Guid roleId, IEnumerable<RolePermission> permissions, Guid actionByUserId, string actionByUserName, Guid? targetUserId = null, string? targetUserName = null)
    {
        // 0. Get Role's CompanyId first to ensure correct tenant association
        var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roleId);
        var companyId = role?.CompanyId;
        var incomingUserId = permissions.FirstOrDefault()?.UserId;

        var existingPermissions = incomingUserId.HasValue
            ? await _context.RolePermissions.Where(rp => rp.RoleId == roleId && rp.UserId == incomingUserId.Value).ToListAsync()
            : await _context.RolePermissions.Where(rp => rp.RoleId == roleId && rp.UserId == null).ToListAsync();

        // 1. Process Updates and Inserts
        var incomingBranchIds = permissions.Select(p => p.BranchId).Distinct().ToList();

        foreach (var incoming in permissions)
        {
            var existing = existingPermissions.FirstOrDefault(p => 
                p.MenuId == incoming.MenuId && 
                ((p.BranchId == null && incoming.BranchId == null) || 
                 (p.BranchId != null && incoming.BranchId != null && p.BranchId.Equals(incoming.BranchId, StringComparison.OrdinalIgnoreCase))));
            
            if (existing != null)
            {
                // Update existing record
                existing.UpdatePermissions(incoming.CanView, incoming.CanAdd, incoming.CanEdit, incoming.CanDelete, incoming.AdditionalActions);
                existing.CompanyId = companyId; // Force sync CompanyId
                existing.UserId = incomingUserId; // Force sync UserId
                _context.RolePermissions.Update(existing);
            }
            else
            {
                // Add new record
                incoming.RoleId = roleId;
                incoming.CompanyId = companyId; // Set CompanyId from Role
                incoming.UserId = incomingUserId; // Set UserId
                await _context.RolePermissions.AddAsync(incoming);
            }
        }

        // 2. Process Deletions (Only for the branches being updated)
        var toRemove = existingPermissions.Where(p => 
            incomingBranchIds.Any(ib => (p.BranchId == null && ib == null) || (p.BranchId != null && ib != null && p.BranchId.Equals(ib, StringComparison.OrdinalIgnoreCase))) && 
            !permissions.Any(ip => ip.MenuId == p.MenuId && 
                                   ((p.BranchId == null && ip.BranchId == null) || 
                                    (p.BranchId != null && ip.BranchId != null && p.BranchId.Equals(ip.BranchId, StringComparison.OrdinalIgnoreCase))))).ToList();

        if (toRemove.Any())
        {
            _context.RolePermissions.RemoveRange(toRemove);
        }

        // 3. Create Audit Log
        var details = $"Updated permissions for Role: {role?.RoleName ?? roleId.ToString()}";
        if (incomingUserId.HasValue || targetUserId.HasValue)
        {
            details += $", User: {targetUserName ?? targetUserId?.ToString() ?? incomingUserId?.ToString()}";
        }
        
        var auditLog = new PermissionAuditLog(
            actionByUserId,
            actionByUserName,
            "Updated Permissions",
            details,
            targetUserId ?? incomingUserId,
            targetUserName,
            roleId,
            role?.RoleName,
            companyId
        );

        await _context.PermissionAuditLogs.AddAsync(auditLog);

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Application.DTOs.UserPermissionDto>> GetAggregatedPermissionsAsync(List<Guid> roleIds, Guid? userId = null, string? fallbackBranchId = null)
    {
        // 🚀 PLATFORM ADMIN BYPASS: Return all menus with full permissions
        bool isPlatformAdmin = _currentUserService.IsPlatformAdmin;
        if (!isPlatformAdmin && userId.HasValue)
        {
            var userEmail = await _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.Id == userId.Value)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            isPlatformAdmin = userEmail != null && userEmail.Equals("Default_Admin@gmail.com", StringComparison.OrdinalIgnoreCase);
        }

        if (isPlatformAdmin)
        {
            var allMenus = await _context.Menus
                .IgnoreQueryFilters()
                .ToListAsync();

            return allMenus.Select(m => new Application.DTOs.UserPermissionDto
            {
                MenuId = m.Id,
                MenuName = m.Title,
                ActionCode = m.Url ?? string.Empty,
                CanView = true,
                CanAdd = true,
                CanEdit = true,
                CanDelete = true,
                AdditionalActions = null
            });
        }

        var roleNames = await _context.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.RoleName.ToLower())
            .ToListAsync();
            
        bool isAdmin = roleNames.Any(n => n.Contains("admin"));

        var activeBranchId = _currentUserService.BranchId;
        if (string.IsNullOrEmpty(activeBranchId) || activeBranchId == "null")
        {
            activeBranchId = fallbackBranchId;
        }

        var query = _context.RolePermissions
            .IgnoreQueryFilters()
            .Include(rp => rp.Menu)
            .Where(rp => (rp.RoleId.HasValue && roleIds.Contains(rp.RoleId.Value)) || (userId.HasValue && rp.UserId == userId.Value));

        if (!_currentUserService.IsPlatformAdmin && !string.IsNullOrEmpty(activeBranchId) && activeBranchId != "null" && activeBranchId != "undefined")
        {
            var branchIds = activeBranchId.Split(',').Select(b => b.Trim().ToLower()).ToList();
            query = query.Where(rp => rp.BranchId == null || rp.BranchId == "" || rp.BranchId.ToLower() == "global" || branchIds.Contains(rp.BranchId.ToLower()));
        }

        var dbPermissions = await query.ToListAsync();

        var activeBranchIdsList = !string.IsNullOrEmpty(activeBranchId) && activeBranchId != "null" && activeBranchId != "undefined"
            ? activeBranchId.Split(',').Select(b => b.Trim().ToLower()).ToList()
            : new List<string>();

        // Local helper to find the best permission record for a target (prioritizing branch-specific over Global)
        RolePermission? GetBestPermission(IEnumerable<RolePermission> permissionsList)
        {
            if (_currentUserService.IsPlatformAdmin)
            {
                return permissionsList.FirstOrDefault();
            }

            if (activeBranchIdsList.Any())
            {
                var branchSpecific = permissionsList.FirstOrDefault(p => p.BranchId != null && activeBranchIdsList.Contains(p.BranchId.ToLower()));
                if (branchSpecific != null) return branchSpecific;
            }

            return permissionsList.FirstOrDefault(p => p.BranchId == null || p.BranchId == "" || p.BranchId.ToLower() == "global");
        }

        var groupedByMenu = dbPermissions.GroupBy(rp => rp.MenuId);
        var resultList = new List<Application.DTOs.UserPermissionDto>();

        foreach (var group in groupedByMenu)
        {
            var menuId = group.Key;

            // 1. User Specific override match check
            var userSpecificsForMenu = userId.HasValue
                ? dbPermissions.Where(rp => rp.UserId == userId.Value && rp.MenuId == menuId).ToList()
                : new List<RolePermission>();

            var bestUserPerms = userSpecificsForMenu
                .GroupBy(rp => rp.RoleId)
                .Select(g => GetBestPermission(g))
                .Where(p => p != null)
                .Cast<RolePermission>()
                .ToList();

            if (bestUserPerms.Any())
            {
                var actions = bestUserPerms
                    .Where(x => !string.IsNullOrEmpty(x.AdditionalActions))
                    .SelectMany(x => x.AdditionalActions!.Split(','))
                    .Select(a => a.Trim())
                    .Distinct()
                    .ToList();

                var additionalActionsStr = actions.Any() ? string.Join(",", actions) : null;
                var firstUserPerm = bestUserPerms.First();

                resultList.Add(new Application.DTOs.UserPermissionDto
                {
                    MenuId = menuId,
                    MenuName = firstUserPerm.Menu?.Title ?? string.Empty,
                    ActionCode = firstUserPerm.Menu?.Url ?? string.Empty, 
                    CanView = bestUserPerms.Any(x => x.CanView),
                    CanAdd = bestUserPerms.Any(x => x.CanAdd),
                    CanEdit = bestUserPerms.Any(x => x.CanEdit),
                    CanDelete = bestUserPerms.Any(x => x.CanDelete),
                    AdditionalActions = additionalActionsStr
                });
            }
            else
            {
                // 2. Role Specific matching check
                var rolePerms = dbPermissions.Where(rp => rp.UserId == null && rp.MenuId == menuId).ToList();
                var bestRolePerms = rolePerms
                    .GroupBy(rp => rp.RoleId)
                    .Select(g => GetBestPermission(g))
                    .Where(p => p != null)
                    .Cast<RolePermission>()
                    .ToList();

                if (bestRolePerms.Any())
                {
                    var actions = bestRolePerms
                        .Where(x => !string.IsNullOrEmpty(x.AdditionalActions))
                        .SelectMany(x => x.AdditionalActions!.Split(','))
                        .Select(a => a.Trim())
                        .Distinct()
                        .ToList();

                    var additionalActionsStr = actions.Any() ? string.Join(",", actions) : null;
                    var firstRolePerm = bestRolePerms.First();

                    resultList.Add(new Application.DTOs.UserPermissionDto
                    {
                        MenuId = menuId,
                        MenuName = firstRolePerm.Menu?.Title ?? string.Empty,
                        ActionCode = firstRolePerm.Menu?.Url ?? string.Empty, 
                        CanView = bestRolePerms.Any(x => x.CanView),
                        CanAdd = bestRolePerms.Any(x => x.CanAdd),
                        CanEdit = bestRolePerms.Any(x => x.CanEdit),
                        CanDelete = bestRolePerms.Any(x => x.CanDelete),
                        AdditionalActions = additionalActionsStr
                    });
                }
            }
        }

        return resultList;
    }

    public async Task AddAsync(RolePermission permission)
    {
        await _context.RolePermissions.AddAsync(permission);
    }
}
