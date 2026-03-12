namespace Inventory.Domain.Entities
{
    public class InventoryTransaction
    {
        public Guid Id { get; private set; }

        public Guid ProductId { get; private set; }
        public decimal Quantity { get; private set; }

        public string TransactionType { get; private set; } = null!;
        public string ReferenceId { get; private set; } = null!;
        public Guid? WarehouseId { get; private set; }
        public Guid? RackId { get; private set; }

        public DateTime? MfgDate { get; private set; }
        public DateTime? ExpDate { get; private set; }

        public DateTime CreatedOn { get; private set; }

        protected InventoryTransaction() { }

        public InventoryTransaction(
            Guid productId,
            decimal quantity,
            string transactionType,
            string referenceId,
            Guid? warehouseId = null,
            Guid? rackId = null,
            DateTime? mfgDate = null,
            DateTime? expDate = null)
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
            CreatedOn = DateTime.UtcNow;
        }
    }
}
