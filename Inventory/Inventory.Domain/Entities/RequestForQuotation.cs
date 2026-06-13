using System;
using System.Collections.Generic;
using Inventory.Domain.Common;
using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities;

public class RequestForQuotation : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RfqNo { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public RfqStatus Status { get; set; } = RfqStatus.Draft;
    public string? Remarks { get; set; }
    public bool IsQuick { get; set; } = false;

    public virtual ICollection<RequestForQuotationItem> Items { get; set; } = new List<RequestForQuotationItem>();
}

