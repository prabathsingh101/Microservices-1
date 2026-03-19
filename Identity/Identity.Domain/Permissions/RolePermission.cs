using Identity.Domain.Entities;
using Identity.Domain.Roles;
using Identity.Domain.Menus;
using System.ComponentModel.DataAnnotations;

namespace Identity.Domain.Permissions;

public class RolePermission
{
    [Key]
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int MenuId { get; set; }
    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public string? AdditionalActions { get; set; } // Added to support custom buttons like BULK_ADD

    public Role? Role { get; private set; }
    public Menu? Menu { get; private set; }

    public RolePermission(int roleId, int menuId, bool canView, bool canAdd, bool canEdit, bool canDelete, string? additionalActions = null)
    {
        RoleId = roleId;
        MenuId = menuId;
        CanView = canView;
        CanAdd = canAdd;
        CanEdit = canEdit;
        CanDelete = canDelete;
        AdditionalActions = additionalActions;
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
