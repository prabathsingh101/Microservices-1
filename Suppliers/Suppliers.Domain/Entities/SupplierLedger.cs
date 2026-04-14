using Suppliers.Domain.Common;
using System;

namespace Suppliers.Domain.Entities
{
    public class SupplierLedger : BaseAuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SupplierId { get; set; }
        public string TransactionType { get; set; } = string.Empty; // Purchase, Return, Payment
        public string ReferenceId { get; set; } = string.Empty; // Invoice No or Payment Id
        public decimal Debit { get; set; } // Payments/Returns
        public decimal Credit { get; set; } // Purchases
        public decimal Balance { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Description { get; set; }
    }
}
