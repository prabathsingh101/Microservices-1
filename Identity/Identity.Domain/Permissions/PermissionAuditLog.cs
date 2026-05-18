using System;
using System.ComponentModel.DataAnnotations;
using Identity.Domain.Common;

namespace Identity.Domain.Permissions;

public class PermissionAuditLog : AuditableEntity, IMultiTenant
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ActionByUserId { get; set; }
    public string ActionByUserName { get; set; } = string.Empty;
    
    public Guid? TargetUserId { get; set; }
    public string? TargetUserName { get; set; }
    
    public Guid? TargetRoleId { get; set; }
    public string? TargetRoleName { get; set; }
    
    public string Action { get; set; } = string.Empty; // "Updated Permissions", "Cloned Role"
    public string Details { get; set; } = string.Empty; // JSON or text summary
    
    public Guid? CompanyId { get; set; } // For Multi-Tenancy
    public string? BranchId { get; set; } // Required by IMultiTenant

    public PermissionAuditLog(Guid actionByUserId, string actionByUserName, string action, string details, Guid? targetUserId = null, string? targetUserName = null, Guid? targetRoleId = null, string? targetRoleName = null, Guid? companyId = null, string? branchId = null)
    {
        Id = Guid.NewGuid();
        ActionByUserId = actionByUserId;
        ActionByUserName = actionByUserName;
        Action = action;
        Details = details;
        TargetUserId = targetUserId;
        TargetUserName = targetUserName;
        TargetRoleId = targetRoleId;
        TargetRoleName = targetRoleName;
        CompanyId = companyId;
        BranchId = branchId;
    }

    public PermissionAuditLog() { }
}
