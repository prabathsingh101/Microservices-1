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
        public decimal RemainingStock { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? WarehouseName { get; set; }
        public string? RackName { get; set; }
        public string? BranchId { get; set; }
    }
}
