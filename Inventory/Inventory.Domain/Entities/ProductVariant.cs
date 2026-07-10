using System;
using Inventory.Domain.Common;

namespace Inventory.Domain.Entities
{
    public class ProductVariant : BaseAuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string? SKU { get; set; }
        
        public decimal AdditionalPrice { get; set; } = 0;
        public decimal CurrentStock { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}
