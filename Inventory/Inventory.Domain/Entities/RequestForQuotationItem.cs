using System;
using Inventory.Domain.Common;

namespace Inventory.Domain.Entities;

public class RequestForQuotationItem : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RfqId { get; set; }
    public virtual RequestForQuotation RequestForQuotation { get; set; } = null!;
    public Guid ProductId { get; set; }
    public virtual Product? Product { get; set; }
    public decimal Qty { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? Discount { get; set; }
    public decimal? TotalCost { get; set; }
}
