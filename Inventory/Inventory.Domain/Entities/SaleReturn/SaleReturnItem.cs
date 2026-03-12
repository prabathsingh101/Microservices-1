using Inventory.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Domain.Entities;

public class SaleReturnItem : BaseAuditableEntity
{
    [Key]
    public int SaleReturnItemId { get; set; }
    public int SaleReturnHeaderId { get; set; }
    public virtual SaleReturnHeader SaleReturnHeader { get; set; } = null!;

    [Required]
    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    [Required]
    public decimal ReturnQty { get; set; } // Ye quantity stock mein wapas jayegi

    [Required]
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }

    public decimal TaxPercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Business Logic Fields
    public string? Reason { get; set; } // Damaged, Wrong Delivery, Customer Unsatisfied
    public string? ItemCondition { get; set; } // Restockable ya Scrapped
    public DateTime? MfgDate { get; set; }
    public DateTime? ExpDate { get; set; }
}