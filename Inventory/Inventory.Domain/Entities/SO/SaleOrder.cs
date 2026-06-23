using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Inventory.Domain.Common;

namespace Inventory.Domain.Entities.SO
{
    public class SaleOrder : BaseAuditableEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(50)]
        public string SONumber { get; set; } = string.Empty; // Unique Order Number [cite: 3]

        public Guid? CustomerId { get; set; } // Customer table se linked [cite: 3]
        public Guid? PriceListId { get; set; } // Price List linked [cite: 3]

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

        public bool IsQuick { get; set; } // Flag for Quick vs Standard Sale
        public string? GuestName { get; set; }
        public string? GuestPhone { get; set; }
        
        [StringLength(500)]
        public string? CancelReason { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorRegNo { get; set; }

        [StringLength(50)]
        public string? DeliveryType { get; set; } // StorePickup, HomeDelivery
        
        [StringLength(500)]
        public string? DeliveryAddress { get; set; }
        
        [StringLength(100)]
        public string? DeliverySlot { get; set; }
        
        [StringLength(100)]
        public string? DeliveryBoyId { get; set; }
        
        [StringLength(200)]
        public string? DeliveryBoyName { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal DeliveryCharges { get; set; } = 0;
        
        [StringLength(50)]
        public string? DeliveryStatus { get; set; } // Pending, Assigned, OutForDelivery, Delivered, Returned, Cancelled
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CodCollectedAmount { get; set; }
        
        [StringLength(50)]
        public string? CodPaymentMode { get; set; } // Cash, UPI
        
        public bool CashSettled { get; set; } = false;
        public DateTime? CashSettledDate { get; set; }
        
        [StringLength(200)]
        public string? CashSettledBy { get; set; }

        // Relationship: One SaleOrder has many SaleOrderItems
        public virtual ICollection<SaleOrderItem> Items { get; set; }
    }
}
