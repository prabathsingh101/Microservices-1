using Customers.Domain.Common;
using System;

namespace Customers.Domain.Entities
{
    public class CustomerLedger : BaseAuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CustomerId { get; set; }
        public string TransactionType { get; set; } = string.Empty; // Sale, Return, Receipt
        public string ReferenceId { get; set; } = string.Empty; // Invoice No or Receipt Id
        public decimal Debit { get; set; } // Sales
        public decimal Credit { get; set; } // Receipts/Returns
        public decimal Balance { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Description { get; set; }
    }
}
