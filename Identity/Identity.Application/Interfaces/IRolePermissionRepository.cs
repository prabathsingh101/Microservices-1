using Identity.Domain.Permissions;

namespace Identity.Application.Interfaces;

public interface IRolePermissionRepository
{
    Task<IEnumerable<RolePermission>> GetPermissionsByRoleIdAsync(Guid roleId, Guid? userId = null);
    Task UpdateRolePermissionsAsync(Guid roleId, IEnumerable<RolePermission> permissions);
    Task<IEnumerable<DTOs.UserPermissionDto>> GetAggregatedPermissionsAsync(List<Guid> roleIds, Guid? userId = null);
    Task AddAsync(RolePermission permission);
}
