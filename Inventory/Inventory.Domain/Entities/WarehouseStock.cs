using Inventory.Domain.Common;

namespace Inventory.Domain.Entities
{
    public class WarehouseStock : BaseAuditableEntity
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid WarehouseId { get; set; }
        public decimal Quantity { get; set; }
        public decimal MinStock { get; set; }

        // Navigation properties
        public virtual Product? Product { get; set; }
        public virtual Warehouse? Warehouse { get; set; }
    }
}
