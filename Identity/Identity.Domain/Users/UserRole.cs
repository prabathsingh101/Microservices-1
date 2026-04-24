using Identity.Domain.Roles;

namespace Identity.Domain.Users;

public class UserRole : Identity.Domain.Common.IMultiTenant
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid? CompanyId { get; set; }
    public string? BranchId { get; set; }

    public User User { get; private set; } = null!;
    public Role Role { get; private set; } = null!;

    private UserRole() { }

    public UserRole(Guid userId, Guid roleId, Guid? companyId = null, string? branchId = null)
    {
        UserId = userId;
        RoleId = roleId;
        CompanyId = companyId;
        BranchId = branchId;
    }
}
