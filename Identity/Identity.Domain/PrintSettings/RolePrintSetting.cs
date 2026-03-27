using System.ComponentModel.DataAnnotations;
using Identity.Domain.Roles;

namespace Identity.Domain.PrintSettings;

public class RolePrintSetting
{
    [Key]
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string PageName { get; set; } = string.Empty;
    public string PrintFormat { get; set; } = string.Empty; // "A4" or "THERMAL"

    public Role? Role { get; private set; }

    public RolePrintSetting(int roleId, string pageName, string printFormat)
    {
        RoleId = roleId;
        PageName = pageName;
        PrintFormat = printFormat;
    }

    public RolePrintSetting() { }

    public void UpdateFormat(string printFormat)
    {
        PrintFormat = printFormat;
    }
}
