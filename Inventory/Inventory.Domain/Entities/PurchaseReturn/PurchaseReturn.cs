using Inventory.Domain.Common;

namespace Inventory.Domain.Entities;

public class PurchaseReturn : BaseAuditableEntity
{
    public Guid Id { get; set; } // Primary Key
    public string ReturnNumber { get; set; } // Auto-generated: e.g., PR-2026-0001 [cite: 2026-02-03]
    public DateTime ReturnDate { get; set; }
    public Guid SupplierId { get; set; } // Changed to Guid
    public decimal SubTotal { get; set; } // Bina tax ke total
    public decimal TotalTax { get; set; } // Kul Tax amount
    public decimal GrandTotal { get; set; }
    public string Remarks { get; set; }
    public string Status { get; set; } // "Draft" or "Confirmed" [cite: 2026-02-03]
    public string? GatePassNo { get; set; }
    public bool IsQuick { get; set; } // Flag to identify quick return workflow


    // Navigation Property
    public ICollection<PurchaseReturnItem> Items { get; set; }
}
