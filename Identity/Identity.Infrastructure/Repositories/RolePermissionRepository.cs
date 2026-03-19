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

    public async Task<IEnumerable<RolePermission>> GetPermissionsByRoleIdAsync(int roleId)
    {
        return await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();
    }

    public async Task UpdateRolePermissionsAsync(int roleId, IEnumerable<RolePermission> permissions)
    {
        var existingPermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        // 1. Process Updates and Inserts
        foreach (var incoming in permissions)
        {
            var existing = existingPermissions.FirstOrDefault(p => p.MenuId == incoming.MenuId);
            if (existing != null)
            {
                // Update existing record
                existing.UpdatePermissions(incoming.CanView, incoming.CanAdd, incoming.CanEdit, incoming.CanDelete, incoming.AdditionalActions);
                _context.RolePermissions.Update(existing);
            }
            else
            {
                // Add new record
                incoming.RoleId = roleId;
                incoming.Id = 0; // Ensure it's treated as new
                await _context.RolePermissions.AddAsync(incoming);
            }
        }

        // 2. Process Deletions
        var incomingMenuIds = permissions.Select(p => p.MenuId).ToList();
        var toRemove = existingPermissions.Where(p => !incomingMenuIds.Contains(p.MenuId)).ToList();
        if (toRemove.Any())
        {
            _context.RolePermissions.RemoveRange(toRemove);
        }

        await _context.SaveChangesAsync();
    }

}
