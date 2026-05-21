using Customers.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace Customers.Domain.Entities
{
    public class ContraEntry : BaseAuditableEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime TransferDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string SourceType { get; set; } = string.Empty; // 'Cash' or 'Bank'

        [MaxLength(250)]
        public string? SourceAccount { get; set; } // 'Main Cash Box' or 'HDFC Bank Account'

        [Required]
        [MaxLength(50)]
        public string DestinationType { get; set; } = string.Empty; // 'Cash' or 'Bank'

        [MaxLength(250)]
        public string? DestinationAccount { get; set; } // 'Main Cash Box' or 'HDFC Bank Account'

        public decimal Amount { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        public string? Remarks { get; set; }
    }
}
