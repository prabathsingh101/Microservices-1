using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseOrders.DTOs
{
    public class PendingPODto
    {
        public Guid Id { get; set; }
        public string PoNumber { get; set; }
        public string SupplierName { get; set; }
        public DateTime PoDate { get; set; }
        public string Status { get; set; }
        public decimal ExpectedQty { get; set; }
    }
}
