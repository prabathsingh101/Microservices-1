using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseOrders.DTOs
{
    public class BulkDeleteRequestDto
    {
        public Guid PurchaseOrderId { get; set; }
        public List<Guid> ItemIds { get; set; } = new();
    }
}
