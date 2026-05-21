using Customers.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Customers.Domain.Entities
{
    public class BankStatement : BaseAuditableEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(250)]
        public string FileName { get; set; } = string.Empty;

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(100)]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string BankName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // 'Pending', 'Partially Reconciled', 'Reconciled'

        public decimal TotalAmount { get; set; }

        public virtual ICollection<BankStatementLine> Lines { get; set; } = new List<BankStatementLine>();
    }
}
