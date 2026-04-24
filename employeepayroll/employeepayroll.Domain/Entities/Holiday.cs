using employeepayroll.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace employeepayroll.Domain.Entities;

public class Holiday : BaseAuditableEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string? HolidayName { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public string? Description { get; set; }

    public bool IsRecursive { get; set; } = false; // Optional, to specify recurring holidays like Dec 25
}
