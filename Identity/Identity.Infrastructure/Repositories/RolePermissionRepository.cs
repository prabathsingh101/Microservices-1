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

    public async Task<IEnumerable<RolePermission>> GetPermissionsByRoleIdAsync(Guid roleId)
    {
        return await _context.RolePermissions
            .IgnoreQueryFilters()
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();
    }

    public async Task UpdateRolePermissionsAsync(Guid roleId, IEnumerable<RolePermission> permissions)
    {
        // 0. Get Role's CompanyId first to ensure correct tenant association
        var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roleId);
        var companyId = role?.CompanyId;

        var existingPermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

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
                _context.RolePermissions.Update(existing);
            }
            else
            {
                // Add new record
                incoming.RoleId = roleId;
                incoming.CompanyId = companyId; // Set CompanyId from Role
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

    public async Task<IEnumerable<Application.DTOs.UserPermissionDto>> GetAggregatedPermissionsAsync(List<Guid> roleIds)
    {
        var dbPermissions = await _context.RolePermissions
            .IgnoreQueryFilters()
            .Include(rp => rp.Menu)
            .Where(rp => roleIds.Contains(rp.RoleId))
            .ToListAsync();

        return dbPermissions
            .GroupBy(rp => rp.MenuId)
            .Select(g => {
                var first = g.First();
                
                // Aggregate distinct actions from all roles
                var actions = g
                    .Where(x => !string.IsNullOrEmpty(x.AdditionalActions))
                    .SelectMany(x => x.AdditionalActions!.Split(','))
                    .Select(a => a.Trim())
                    .Distinct()
                    .ToList();

                var additionalActionsStr = actions.Any() ? string.Join(",", actions) : null;

                return new Application.DTOs.UserPermissionDto
                {
                    MenuName = first.Menu!.Title,
                    ActionCode = first.Menu!.Url ?? string.Empty, 
                    CanView = g.Any(x => x.CanView),
                    CanAdd = g.Any(x => x.CanAdd),
                    CanEdit = g.Any(x => x.CanEdit),
                    CanDelete = g.Any(x => x.CanDelete),
                    AdditionalActions = additionalActionsStr
                };
            })
            .ToList();
    }

    public async Task AddAsync(RolePermission permission)
    {
        await _context.RolePermissions.AddAsync(permission);
    }
}
