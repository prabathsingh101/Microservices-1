using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Inventory.Domain.Common;

namespace Inventory.Domain.Entities.SalesInvoice
{
    public class DeliveryChallanItem : BaseAuditableEntity, IMultiTenant
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? DeliveryChallanId { get; set; }
        
        public Guid? ProductId { get; set; }

        [StringLength(255)]
        public string? ProductName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Qty { get; set; }

        [StringLength(50)]
        public string? Unit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Rate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MRP { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPercent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GSTPercent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Total { get; set; }

        public Guid? WarehouseId { get; set; }
        
        public Guid? RackId { get; set; }
        
        [StringLength(100)]
        public string? BatchNumber { get; set; }
        
        public DateTime? MfgDate { get; set; }
        
        public DateTime? ExpDate { get; set; }

        public Guid? CompanyId { get; set; }
        
        public string? BranchId { get; set; }

        // Navigation Property
        [ForeignKey("DeliveryChallanId")]
        public virtual DeliveryChallan? DeliveryChallan { get; set; }
    }
}
