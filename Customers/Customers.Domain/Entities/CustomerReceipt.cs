using Customers.Domain.Common;
using System;

namespace Customers.Domain.Entities
{
    public class CustomerReceipt : BaseAuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? CustomerId { get; set; }
        public decimal Amount { get; set; }
        public DateTime ReceiptDate { get; set; }
        public string ReceiptMode { get; set; } // GPay, Cash, Check, etc.
        public string? ReferenceNumber { get; set; }
        public string? Remarks { get; set; }
        public string? ChequeNumber { get; set; }
        public DateTime? ChequeDate { get; set; }
        public string? BankName { get; set; }
        public string? BankBranch { get; set; }
        public string? BankAddress { get; set; }
    }
}
