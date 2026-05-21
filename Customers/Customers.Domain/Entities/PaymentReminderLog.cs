using Customers.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace Customers.Domain.Entities
{
    public class PaymentReminderLog : BaseAuditableEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CustomerId { get; set; }
        
        [Required]
        [MaxLength(250)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        public decimal OutstandingAmount { get; set; }

        [Required]
        [MaxLength(50)]
        public string ReminderType { get; set; } = string.Empty; // 'WhatsApp' or 'SMS'

        [Required]
        [MaxLength(50)]
        public string SentStatus { get; set; } = string.Empty; // 'Success', 'Failed'

        public string? SentMessage { get; set; }
    }
}
