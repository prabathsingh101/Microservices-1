using Inventory.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseOrders.Commands.Delete
{
    // Application/Handlers/BulkDeletePurchaseOrderHandler.cs
    public class BulkDeletePurchaseOrderHandler : IRequestHandler<BulkDeletePurchaseOrderCommand, bool>
    {
        private readonly IPurchaseOrderRepository _repo;
        private readonly IUnitOfWork _uow;

        public BulkDeletePurchaseOrderHandler(IPurchaseOrderRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task<bool> Handle(BulkDeletePurchaseOrderCommand request, CancellationToken ct)
        {
            if (request.Ids == null || !request.Ids.Any()) return false;

            // Saare selected orders ek saath fetch karna
            var orders = await _repo.GetByIdsAsync(request.Ids);

            foreach (var order in orders)
            {
                // Hamara Domain Rule: Draft mode check
                // Agar ek bhi PO 'Received' hua toh ye exception throw kar dega
                order.CanBeDeleted();

                // Repository ko bolna ki isey delete list mein daal de
                _repo.Delete(order);
            }

            // Saare records ek hi transaction mein commit honge
            return await _uow.SaveChangesAsync(ct) > 0;
        }
    }
}
