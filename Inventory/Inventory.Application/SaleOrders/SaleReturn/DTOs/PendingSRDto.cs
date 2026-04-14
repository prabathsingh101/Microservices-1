using System;

namespace Inventory.Application.SaleOrders.SaleReturn.DTOs
{
    public class PendingSRDto
    {
        public Guid Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalQty { get; set; }
    }
}
