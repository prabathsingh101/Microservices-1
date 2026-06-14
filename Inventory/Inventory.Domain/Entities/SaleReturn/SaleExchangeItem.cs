using Inventory.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Domain.Entities
{
    public class SaleExchangeItem : BaseAuditableEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SaleReturnHeaderId { get; set; }
        public virtual SaleReturnHeader SaleReturnHeader { get; set; } = null!;

        [Required]
        public Guid ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        [Required]
        public decimal Qty { get; set; } // Quantity issued (Stock Out)

        [Required]
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }

        [Required]
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public DateTime? MfgDate { get; set; }
        public DateTime? ExpDate { get; set; }
        
        public Guid? WarehouseId { get; set; }
        public Guid? RackId { get; set; }
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? BatchNumber { get; set; }
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? ReferenceNumber { get; set; }
    }
}
