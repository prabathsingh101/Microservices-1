using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities.SO
{
    public class SaleOrder
    {
        [Key]
       
        public int Id { get; set; } // Primary Key [cite: 3]

        [Required]
        [StringLength(50)]
        public string SONumber { get; set; } = string.Empty; // Unique Order Number [cite: 3]

        [Required]
        public int CustomerId { get; set; } // Customer table se linked [cite: 3]

        [Required]
        public DateTime SODate { get; set; } // Order date [cite: 3]

        public DateTime? ExpectedDeliveryDate { get; set; } // Kab tak deliver karna hai [cite: 3]

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; } // Without Tax amount [cite: 3]

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalTax { get; set; } // GST ka total amount [cite: 3]

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; } // Final payable amount [cite: 3]

        public string? TaxType { get; set; } // local, interState
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TdsPercent { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TdsAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TcsPercent { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TcsAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? IgstAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CgstAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? SgstAmount { get; set; }

        [Required]
        public string Remarks { get; set; } = string.Empty; // Terms and conditions [cite: 3]

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty; // Draft, Confirmed, etc. [cite: 3]
        public string? GatePassNo { get; set; }

        [Required]
        public string CreatedBy { get; set; } = string.Empty; // Login user ki email/ID [cite: 3]
        public bool IsQuick { get; set; } // Flag for Quick vs Standard Sale
        public DateTime CreatedAt { get; set; } // Record creation time [cite: 3]

        // Relationship: One SaleOrder has many SaleOrderItems
        public virtual ICollection<SaleOrderItem> Items { get; set; }
    }
}