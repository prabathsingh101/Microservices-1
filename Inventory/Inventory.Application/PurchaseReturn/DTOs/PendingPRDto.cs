using System;

namespace Inventory.Application.PurchaseReturn.DTOs
{
    public class PendingPRDto
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalQty { get; set; }
    }
}
