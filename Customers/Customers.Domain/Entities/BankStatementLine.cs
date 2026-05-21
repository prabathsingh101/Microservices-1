using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Customers.Domain.Entities
{
    public class BankStatementLine
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid BankStatementId { get; set; }

        [ForeignKey(nameof(BankStatementId))]
        public virtual BankStatement? BankStatement { get; set; }

        public DateTime TransactionDate { get; set; }

        public string? Description { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        public decimal Withdrawal { get; set; } = 0;

        public decimal Deposit { get; set; } = 0;

        [Required]
        [MaxLength(50)]
        public string ReconciliationStatus { get; set; } = "Unmatched"; // 'Unmatched', 'Matched'

        [MaxLength(50)]
        public string? MatchedTransactionType { get; set; } // 'CustomerReceipt', 'SupplierPayment', 'ExpenseEntry'

        public Guid? MatchedTransactionId { get; set; }
    }
}
