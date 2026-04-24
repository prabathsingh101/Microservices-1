using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using employeepayroll.Domain.Enums;

using employeepayroll.Domain.Common;

namespace employeepayroll.Domain.Entities;

public class Leave : BaseAuditableEntity
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

}
