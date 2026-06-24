using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseReturn
{
    public class SupplierSelectDto
    {
        public Guid Id { get; set; } // Supplier ID as Guid
        public string Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? GstIn { get; set; }
    }
}
