using System;

namespace Inventory.Application.PurchaseOrders.DTOs
{
    public class PurchaseOrderLookupDto
    {
        public Guid PurchaseOrderId { get; set; }
        public string PoNumber { get; set; } = string.Empty;
    }
}
