using Inventory.Domain.Common;
using Inventory.Domain.Entities;

public class PurchaseOrderItem : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; } 
    public decimal Qty { get; set; }
    public string Unit { get; set; }
    public decimal Rate { get; set; }
    public decimal DiscountPercent { get; set; }  
    public decimal GstPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal ReceivedQty {  get; set; }
    public DateTime? MfgDate { get; set; }
    public DateTime? ExpDate { get; set; }

    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}
