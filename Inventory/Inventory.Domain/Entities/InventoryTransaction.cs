using Inventory.Domain.Common;

namespace Inventory.Domain.Entities
{
    public class InventoryTransaction : BaseAuditableEntity
    {
        public Guid Id { get; private set; }

        public Guid ProductId { get; private set; }
        public decimal Quantity { get; private set; }

        public string TransactionType { get; private set; } = null!;
        public string ReferenceId { get; private set; } = null!;
        public Guid? WarehouseId { get; private set; }
        public Guid? RackId { get; private set; }
        public string? ReferenceNumber { get; private set; }
        public string? BatchNumber { get; private set; }

        public DateTime? MfgDate { get; private set; }
        public DateTime? ExpDate { get; private set; }
        public DateTime TransactionDate { get; private set; }
        
        // Navigation Properties
        public virtual Product Product { get; set; } = null!;
        public virtual Warehouse? Warehouse { get; set; }
        public virtual Rack? Rack { get; set; }

        protected InventoryTransaction() { }

        public InventoryTransaction(
            Guid productId,
            decimal quantity,
            string transactionType,
            string referenceId,
            Guid? warehouseId = null,
            Guid? rackId = null,
            DateTime? mfgDate = null,
            DateTime? expDate = null,
            Guid? companyId = null,
            string? branchId = null,
            string? referenceNumber = null,
            string? batchNumber = null)
        {
            Id = Guid.NewGuid();
            ProductId = productId;
            Quantity = quantity;
            TransactionType = transactionType;
            ReferenceId = referenceId;
            WarehouseId = warehouseId;
            RackId = rackId;
            MfgDate = mfgDate;
            ExpDate = expDate;
            CompanyId = companyId ?? Guid.Empty;
            BranchId = branchId;
            ReferenceNumber = referenceNumber;
            BatchNumber = batchNumber;
            TransactionDate = DateTime.Now;
        }
    }
}
