using Inventory.Domain.Common;
using Inventory.Domain.Entities.SO;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Domain.Entities;

public class SaleReturnHeader : BaseAuditableEntity // Agar aap BaseAuditableEntity use kar rahe hain
{
    [Key]
    public int SaleReturnHeaderId { get; set; }

    [Required]
    [MaxLength(20)]
    public string ReturnNumber { get; set; } = string.Empty; // Example: SR-2026-0001

    public DateTime ReturnDate { get; set; } = DateTime.Now;

    [Required]
    public int SaleOrderId { get; set; }
    public virtual SaleOrder SaleOrder { get; set; } = null!;

    [Required]
    public int CustomerId { get; set; } // Direct reference for easy reporting

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? Remarks { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Confirmed, Cancelled
    public string? GatePassNo { get; set; }
    public bool IsQuick { get; set; }

    public virtual ICollection<SaleReturnItem> ReturnItems { get; set; } = new List<SaleReturnItem>();
}