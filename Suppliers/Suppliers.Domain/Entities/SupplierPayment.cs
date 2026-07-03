using Suppliers.Domain.Common;
using System;

namespace Suppliers.Domain.Entities
{
    public class SupplierPayment : BaseAuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SupplierId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMode { get; set; } // GPay, Cash, Check, etc.
        public string? ReferenceNumber { get; set; }
        public string? Remarks { get; set; }
        public string TransactionType { get; set; } = "Payment"; // "Payment" or "Refund"
        public string? BankName { get; set; }
        public string? TransactionId { get; set; }
        public string? ChequeNumber { get; set; }
        public DateTime? ChequeDate { get; set; }
    }
}
