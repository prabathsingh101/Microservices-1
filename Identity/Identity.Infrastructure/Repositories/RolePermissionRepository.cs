using Identity.Application.Interfaces;
using Identity.Domain.Permissions;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly IdentityDbContext _context;

    public RolePermissionRepository(IdentityDbContext context)
    {
        _context = context;
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

    public async Task UpdateRolePermissionsAsync(Guid roleId, IEnumerable<RolePermission> permissions)
    {
        // 0. Get Role's CompanyId first to ensure correct tenant association
        var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roleId);
        var companyId = role?.CompanyId;
        var incomingUserId = permissions.FirstOrDefault()?.UserId;

        var existingPermissions = incomingUserId.HasValue
            ? await _context.RolePermissions.Where(rp => rp.UserId == incomingUserId.Value).ToListAsync()
            : await _context.RolePermissions.Where(rp => rp.RoleId == roleId && rp.UserId == null).ToListAsync();

        // 1. Process Updates and Inserts
        var incomingBranchIds = permissions.Select(p => p.BranchId).Distinct().ToList();

        foreach (var incoming in permissions)
        {
            var existing = existingPermissions.FirstOrDefault(p => p.MenuId == incoming.MenuId && p.BranchId == incoming.BranchId);
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
            incomingBranchIds.Contains(p.BranchId) && 
            !permissions.Any(ip => ip.MenuId == p.MenuId && ip.BranchId == p.BranchId)).ToList();

        if (toRemove.Any())
        {
            _context.RolePermissions.RemoveRange(toRemove);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Application.DTOs.UserPermissionDto>> GetAggregatedPermissionsAsync(List<Guid> roleIds, Guid? userId = null)
    {
        var dbPermissions = await _context.RolePermissions
            .IgnoreQueryFilters()
            .Include(rp => rp.Menu)
            .Where(rp => (rp.RoleId.HasValue && roleIds.Contains(rp.RoleId.Value)) || (userId.HasValue && rp.UserId == userId.Value))
            .ToListAsync();

        var userSpecificList = userId.HasValue 
            ? dbPermissions.Where(rp => rp.UserId == userId.Value).ToList() 
            : new List<RolePermission>();

        var groupedByMenu = dbPermissions.GroupBy(rp => rp.MenuId);
        var resultList = new List<Application.DTOs.UserPermissionDto>();

        foreach (var group in groupedByMenu)
        {
            var menuId = group.Key;
            var first = group.First();

            var userSpecificsForMenu = userSpecificList.Where(rp => rp.MenuId == menuId).ToList();

            if (userSpecificsForMenu.Any())
            {
                var actions = userSpecificsForMenu
                    .Where(x => !string.IsNullOrEmpty(x.AdditionalActions))
                    .SelectMany(x => x.AdditionalActions!.Split(','))
                    .Select(a => a.Trim())
                    .Distinct()
                    .ToList();

                var additionalActionsStr = actions.Any() ? string.Join(",", actions) : null;

                resultList.Add(new Application.DTOs.UserPermissionDto
                {
                    MenuName = userSpecificsForMenu.First().Menu!.Title,
                    ActionCode = userSpecificsForMenu.First().Menu!.Url ?? string.Empty, 
                    CanView = userSpecificsForMenu.Any(x => x.CanView),
                    CanAdd = userSpecificsForMenu.Any(x => x.CanAdd),
                    CanEdit = userSpecificsForMenu.Any(x => x.CanEdit),
                    CanDelete = userSpecificsForMenu.Any(x => x.CanDelete),
                    AdditionalActions = additionalActionsStr
                });
            }
            else
            {
                var rolePerms = group.Where(rp => rp.UserId == null).ToList();
                if (rolePerms.Any())
                {
                    var firstRolePerm = rolePerms.First();
                    var actions = rolePerms
                        .Where(x => !string.IsNullOrEmpty(x.AdditionalActions))
                        .SelectMany(x => x.AdditionalActions!.Split(','))
                        .Select(a => a.Trim())
                        .Distinct()
                        .ToList();

                    var additionalActionsStr = actions.Any() ? string.Join(",", actions) : null;

                    resultList.Add(new Application.DTOs.UserPermissionDto
                    {
                        MenuName = firstRolePerm.Menu!.Title,
                        ActionCode = firstRolePerm.Menu!.Url ?? string.Empty, 
                        CanView = rolePerms.Any(x => x.CanView),
                        CanAdd = rolePerms.Any(x => x.CanAdd),
                        CanEdit = rolePerms.Any(x => x.CanEdit),
                        CanDelete = rolePerms.Any(x => x.CanDelete),
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
