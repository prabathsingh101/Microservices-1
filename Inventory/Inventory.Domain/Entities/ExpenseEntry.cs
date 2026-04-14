using Inventory.Domain.Common;

namespace Inventory.Domain.Entities;

public class ExpenseEntry : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public virtual ExpenseCategory? Category { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string PaymentMode { get; set; } = null!; // Cash, Bank, UPI, etc.
    public string? ReferenceNo { get; set; }
    public string? Remarks { get; set; }
    public string? AttachmentPath { get; set; }
}
