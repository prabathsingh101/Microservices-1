using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Inventory.Domain.Common;

namespace Inventory.Domain.Entities.SalesInvoice
{
    public class SalesInvoice : BaseAuditableEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(50)]
        public string InvoiceNo { get; set; } = string.Empty;

        [Required]
        public DateTime InvoiceDate { get; set; }

        public Guid? CustomerId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalTax { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        public string? TaxType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? IgstAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CgstAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? SgstAmount { get; set; }

        [Required]
        public string Remarks { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        public bool IsQuick { get; set; }
        public string? GuestName { get; set; }
        public string? GuestPhone { get; set; }
        public Guid? DeliveryChallanId { get; set; }

        public virtual ICollection<SalesInvoiceItem> Items { get; set; }
    }
}
