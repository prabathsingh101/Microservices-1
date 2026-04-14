using Inventory.Domain.Common;

namespace Inventory.Domain.Entities
{
    public class GRNHeader: BaseAuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string GRNNumber { get; set; }
        public Guid PurchaseOrderId { get; set; } 
        public PurchaseOrder PurchaseOrder { get; set; }    
        public Guid SupplierId { get; set; }
        public DateTime ReceivedDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } // 'Completed' or 'Partial' [cite: 2026-01-22]
        public string? GatePassNo { get; set; }
        public string Remarks { get; set; }
        public bool IsQuick { get; set; } // Flag to identify transaction source (Quick vs PO)
 
        public List<GRNDetail>? GRNItems { get; set; } // Child Items [cite: 2026-01-22]
    }
}
