using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using employeepayroll.Domain.Enums;

namespace employeepayroll.Domain.Entities;

public class Attendance
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [ForeignKey("Employee")]
    public Guid EmployeeId { get; set; }
    public virtual Employee? Employee { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public AttendanceMethod Method { get; set; } = AttendanceMethod.Manual;

    public string? Remarks { get; set; }

    // Audit fields
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedOn { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
