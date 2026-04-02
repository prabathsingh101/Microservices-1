using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using employeepayroll.Domain.Enums;

namespace employeepayroll.Domain.Entities;

public class Leave
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [ForeignKey("Employee")]
    public Guid EmployeeId { get; set; }
    public virtual Employee? Employee { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public LeaveType Type { get; set; }

    [Required]
    public string? Reason { get; set; }

    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

    public string? AdminRemarks { get; set; }

    // Audit fields
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedOn { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
