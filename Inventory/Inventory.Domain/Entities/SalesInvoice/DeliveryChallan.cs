using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Inventory.Domain.Common;

namespace Inventory.Domain.Entities.SalesInvoice
{
    public class DeliveryChallan : BaseAuditableEntity, IMultiTenant
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [StringLength(50)]
        public string? ChallanNo { get; set; }

        public DateTime? ChallanDate { get; set; }

        public Guid? CustomerId { get; set; }
        
        [StringLength(255)]
        public string? CustomerName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalTax { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GrandTotal { get; set; }

        public string? Remarks { get; set; }

        [StringLength(50)]
        public string? Status { get; set; } // Pending, Invoiced, Cancelled

        public string? CancelReason { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GrossWeight { get; set; }

        [StringLength(100)]
        public string? VehicleRegNo { get; set; }

        [StringLength(255)]
        public string? Origin { get; set; }

        [StringLength(255)]
        public string? Destination { get; set; }

        public Guid? CompanyId { get; set; }
        
        public string? BranchId { get; set; }

        public Guid? StockTransferHeaderId { get; set; }

        public virtual ICollection<DeliveryChallanItem> Items { get; set; } = new List<DeliveryChallanItem>();
    }
}
