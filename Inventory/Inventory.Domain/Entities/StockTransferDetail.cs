using System;
using Inventory.Domain.Common;

namespace Inventory.Domain.Entities
{
    public class StockTransferDetail : BaseAuditableEntity
    {
        public Guid Id { get; private set; }
        public Guid StockTransferHeaderId { get; set; }
        public virtual StockTransferHeader StockTransferHeader { get; set; } = null!;

        public Guid ProductId { get; private set; }
        public virtual Product Product { get; private set; } = null!;

        public decimal Quantity { get; private set; }
        public string? BatchNumber { get; private set; }

        private StockTransferDetail() { }

        public StockTransferDetail(
            Guid productId,
            decimal quantity,
            string? batchNumber = null,
            Guid? companyId = null,
            string? branchId = null)
        {
            Id = Guid.NewGuid();
            ProductId = productId;
            Quantity = quantity;
            BatchNumber = batchNumber;
            CompanyId = companyId ?? Guid.Empty;
            BranchId = branchId;
            CreatedOn = DateTime.UtcNow;
        }
    }
}
