using Inventory.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Domain.Entities
{
    public class GRNDetail: BaseAuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GRNHeaderId { get; set; }
        public GRNHeader GRNHeader { get; set; }
        public Guid ProductId { get; set; }    
        public Product Product { get; set; }
        public decimal OrderedQty { get; set; }
        public decimal PendingQty { get; set; }
        public decimal RejectedQty { get; set; }
        public decimal AcceptedQty { get; set; }
        public decimal ReceivedQty { get; set; } // User input quantity [cite: 2026-01-22]
        public decimal UnitRate { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal GstPercent { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
        public Guid? WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }
        public Guid? RackId { get; set; }
        public Rack? Rack { get; set; }
        public DateTime? MfgDate { get; set; }
        public DateTime? ExpDate { get; set; }
        public string? BatchNumber { get; set; }
        public string? ReferenceNumber { get; set; }
        public bool IsSettled { get; set; } = false;
        public bool IsReplacement { get; set; } = false;
    }
}
