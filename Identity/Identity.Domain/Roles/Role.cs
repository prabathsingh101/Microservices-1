using Identity.Domain.Common;

namespace Identity.Domain.Roles;

public class Role : AuditableEntity, IMultiTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string RoleName { get; set; } = default!;
    
    public Guid? CompanyId { get; set; }
    public string? BranchId { get; set; }

    private Role() { } // EF Core

    public Role(string roleName, Guid? companyId = null, string? branchId = null)
    {
        Id = Guid.NewGuid();
        RoleName = roleName;
        CompanyId = companyId;
        BranchId = branchId;
    }

    // Navigation Properties
    public virtual ICollection<Identity.Domain.Permissions.RolePermission> RolePermissions { get; set; } = new List<Identity.Domain.Permissions.RolePermission>();
}
