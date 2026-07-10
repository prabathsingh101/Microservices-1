using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Inventory.Domain.Common;

namespace Inventory.Domain.Entities.SO
{
    public class SaleOrderItem : BaseAuditableEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CompanyId { get; set; }

        [Required]
        public Guid SaleOrderId { get; set; } // SaleOrder link [cite: 6]

        [ForeignKey("SaleOrderId")]
        public virtual SaleOrder SaleOrder { get; set; }

        [Required]
        public Guid ProductId { get; set; } // Product link [cite: 6]
        
        public Guid? ProductVariantId { get; set; }
        [ForeignKey("ProductVariantId")]
        public virtual ProductVariant? ProductVariant { get; set; }

        public string ProductName { get; set; } // Snapshot ke liye [cite: 6]

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Qty { get; set; } // Quantity [cite: 6]

        [Required]
        public string Unit { get; set; } // Unit (PCS, KG, etc.) [cite: 6]

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Rate { get; set; } // Unit rate [cite: 6]

        [Column(TypeName = "decimal(18,2)")]
        public decimal MRP { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountPercent { get; set; } // Item discount [cite: 6]

        [Column(TypeName = "decimal(18,2)")]
        public decimal GSTPercent { get; set; } // Tax percentage [cite: 6]

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } // Calculated GST amount [cite: 6]

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; } // Row final total [cite: 6]

        [Column(TypeName = "datetime2")]
        public DateTime? MfgDate { get; set; } // Manufacturing date

        [Column(TypeName = "datetime2")]
        public DateTime? ExpDate { get; set; } // Expiry date

        public Guid? WarehouseId { get; set; }
        [ForeignKey("WarehouseId")]
        public virtual Warehouse? Warehouse { get; set; }

        public Guid? RackId { get; set; }
        [ForeignKey("RackId")]
        public virtual Rack? Rack { get; set; }

        public string? BatchNumber { get; set; }
        public string? ReferenceNumber { get; set; } // Link to source Purchase/GRN
    }
}
