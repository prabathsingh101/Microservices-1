using Identity.Domain.Permissions;

namespace Identity.Application.Interfaces;

public interface IRolePermissionRepository
{
    Task<IEnumerable<RolePermission>> GetPermissionsByRoleIdAsync(Guid roleId, Guid? userId = null);
    Task UpdateRolePermissionsAsync(Guid roleId, IEnumerable<RolePermission> permissions, Guid actionByUserId, string actionByUserName, Guid? targetUserId = null, string? targetUserName = null);
    Task<IEnumerable<DTOs.UserPermissionDto>> GetAggregatedPermissionsAsync(List<Guid> roleIds, Guid? userId = null, string? fallbackBranchId = null);
    Task AddAsync(RolePermission permission);
}
