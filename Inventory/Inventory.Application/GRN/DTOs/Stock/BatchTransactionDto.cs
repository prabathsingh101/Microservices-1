using System;

namespace Inventory.Application.GRN.DTOs.Stock
{
    public class BatchTransactionDto
    {
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } = null!;
        public string ReferenceId { get; set; } = null!;
        public decimal Quantity { get; set; }
        public string Category { get; set; } = null!; // "IN" or "OUT"
    }
}
