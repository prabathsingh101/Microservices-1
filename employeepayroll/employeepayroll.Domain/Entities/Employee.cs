using employeepayroll.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace employeepayroll.Domain.Entities;

public class Employee : BaseAuditableEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string? EmployeeCode { get; set; } // e.g. EMP001
    
    [Required]
    public string? FullName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? Phone { get; set; }
    
    public string? Designation { get; set; }
    
    public string? Department { get; set; }
    
    public DateTime DateOfJoining { get; set; }
    
    public string? ProfilePicture { get; set; } // Base64 or URL

    public decimal BasicSalary { get; set; }
    public decimal HRA { get; set; }
    public decimal Conveyance { get; set; }
    public decimal SpecialAllowance { get; set; }
    public decimal PF { get; set; }
    public decimal Tax { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Attendance>? Attendances { get; set; }
    public virtual ICollection<Leave>? Leaves { get; set; }
    public virtual ICollection<SalarySlip>? SalarySlips { get; set; }
}
