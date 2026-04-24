using System.ComponentModel.DataAnnotations;
using Identity.Domain.Roles;

namespace Identity.Domain.PrintSettings;

public class RolePrintSetting : Identity.Domain.Common.IMultiTenant
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoleId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? BranchId { get; set; }
    public string PageName { get; set; } = string.Empty;
    public string PrintFormat { get; set; } = string.Empty; // "A4" or "THERMAL"

    public Role? Role { get; private set; }

    public RolePrintSetting(Guid roleId, string pageName, string printFormat, Guid? companyId = null, string? branchId = null)
    {
        Id = Guid.NewGuid();
        RoleId = roleId;
        PageName = pageName;
        PrintFormat = printFormat;
        CompanyId = companyId;
        BranchId = branchId;
    }

    public RolePrintSetting() { }

    public void UpdateFormat(string printFormat)
    {
        PrintFormat = printFormat;
    }
}
