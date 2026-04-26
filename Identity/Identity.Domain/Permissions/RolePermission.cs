using Identity.Domain.Entities;
using Identity.Domain.Roles;
using Identity.Domain.Menus;
using System.ComponentModel.DataAnnotations;
using Identity.Domain.Common;

namespace Identity.Domain.Permissions;

public class RolePermission : AuditableEntity, IMultiTenant
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoleId { get; set; }
    public Guid MenuId { get; set; }
    public Guid? CompanyId { get; set; } // Added for Multi-Tenancy
    public string? BranchId { get; set; }
    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public string? AdditionalActions { get; set; } // Added to support custom buttons like BULK_ADD

    public Role? Role { get; private set; }
    public Menu? Menu { get; private set; }

    public RolePermission(Guid roleId, Guid menuId, bool canView, bool canAdd, bool canEdit, bool canDelete, string? additionalActions = null, Guid? companyId = null)
    {
        Id = Guid.NewGuid();
        RoleId = roleId;
        MenuId = menuId;
        CanView = canView;
        CanAdd = canAdd;
        CanEdit = canEdit;
        CanDelete = canDelete;
        AdditionalActions = additionalActions;
        CompanyId = companyId;
    }

    public RolePermission() { }

    public void UpdatePermissions(bool canView, bool canAdd, bool canEdit, bool canDelete, string? additionalActions = null)
    {
        CanView = canView;
        CanAdd = canAdd;
        CanEdit = canEdit;
        CanDelete = canDelete;
        AdditionalActions = additionalActions;
    }
}
