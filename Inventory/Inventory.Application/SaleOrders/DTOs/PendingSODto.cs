using System;

namespace Inventory.Application.SaleOrders.DTOs
{
    public class PendingSODto
    {
        public Guid Id { get; set; }
        public Guid? CustomerId { get; set; }
        public string SoNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime SoDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalQty { get; set; }
    }
}
