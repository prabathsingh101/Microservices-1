using System;

namespace Inventory.Application.Products.DTOs
{
    public class ProductTransactionDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedOn { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string ReferenceId { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
    }
}
