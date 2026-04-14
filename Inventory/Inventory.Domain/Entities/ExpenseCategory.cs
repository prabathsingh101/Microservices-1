using Inventory.Domain.Common;

namespace Inventory.Domain.Entities;

public class ExpenseCategory : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;


    public virtual ICollection<ExpenseEntry> Expenses { get; set; } = new List<ExpenseEntry>();
}
