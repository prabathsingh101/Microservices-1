using Identity.Domain.Common;

namespace Identity.Domain.Roles;

public class Role : IMultiTenant
{
    public Guid Id { get; set; }

    public string RoleName { get; set; } = default!;
    
    public Guid? CompanyId { get; set; } // NULL = System Role, GUID = Customer Role

    private Role() { } // EF Core

    public Role(string roleName, Guid? companyId = null)
    {
        Id = Guid.NewGuid();
        RoleName = roleName;
        CompanyId = companyId;
    }

    // Navigation Properties
    public virtual ICollection<Identity.Domain.Permissions.RolePermission> RolePermissions { get; set; } = new List<Identity.Domain.Permissions.RolePermission>();
}
