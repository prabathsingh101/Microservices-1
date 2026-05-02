using Identity.Domain.Common;

namespace Identity.Domain.Roles;

public class Role : AuditableEntity, IMultiTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string RoleName { get; set; } = default!;
    
    public Guid? CompanyId { get; set; }
    public string? BranchId { get; set; }
    public string? Description { get; set; } // 🔥 Added Description

    private Role() { } // EF Core

    public Role(string roleName, Guid? companyId = null, string? branchId = null, string? description = null)
    {
        Id = Guid.NewGuid();
        RoleName = roleName;
        CompanyId = companyId;
        BranchId = string.IsNullOrWhiteSpace(branchId) ? null : branchId;
        Description = description;
    }

    // Navigation Properties
    public virtual ICollection<Identity.Domain.Permissions.RolePermission> RolePermissions { get; set; } = new List<Identity.Domain.Permissions.RolePermission>();
}
