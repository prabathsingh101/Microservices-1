using employeepayroll.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace employeepayroll.Domain.Entities;

public class SalarySlip : BaseAuditableEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [ForeignKey("Employee")]
    public Guid EmployeeId { get; set; }
    public virtual Employee? Employee { get; set; }

    [Required]
    public int Month { get; set; }
    [Required]
    public int Year { get; set; }

    // Employee snapshoot
    public string? EmployeeNameSnapshot { get; set; }
    public string? EmployeeCodeSnapshot { get; set; }
    public string? DesignationSnapshot { get; set; }

    // Breakdown
    public decimal BasicSalary { get; set; }
    public decimal HRA { get; set; }
    public decimal Conveyance { get; set; }
    public decimal SpecialAllowance { get; set; }
    public decimal PF { get; set; }
    public decimal Tax { get; set; }

    // Net
    public decimal GrossEarning { get; set; }
    public decimal TotalDeduction { get; set; }
    public decimal NetSalary { get; set; }

    public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;

    // Status
    public string Status { get; set; } = "Paid"; // e.g. Paid, Generated, OnHold
}
