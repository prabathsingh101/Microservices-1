using Inventory.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities;

public class PurchaseReturnItem : BaseAuditableEntity
{
    public Guid Id { get; set; }
    public Guid PurchaseReturnId { get; set; }
    public Guid ProductId { get; set; }
    [NotMapped]
    public string ProductName { get; set; }
    public string GrnRef { get; set; } 
    public decimal ReturnQty { get; set; }
    public decimal Rate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal GstPercent { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TaxAmount { get; set; }
    // Navigation Property
    public PurchaseReturn PurchaseReturn { get; set; }
    public DateTime? MfgDate { get; set; }
    public DateTime? ExpDate { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? ProductVariantId { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.ForeignKey("ProductVariantId")]
    public virtual ProductVariant? ProductVariant { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public Guid? RackId { get; set; }
}
