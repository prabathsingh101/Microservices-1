using Inventory.Application.Common.Interfaces;
using Inventory.Application.PurchaseOrders.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseOrders.Queries.GetPurchaseOrder
{
    public class GetPendingPOHandler : IRequestHandler<GetPendingPOQuery, IEnumerable<PendingPODto>>
    {
        private readonly IPurchaseOrderRepository _repo;
        public GetPendingPOHandler(IPurchaseOrderRepository repo) => _repo = repo;

        public async Task<IEnumerable<PendingPODto>> Handle(GetPendingPOQuery request, CancellationToken ct)
        {
            return await _repo.GetPendingPurchaseOrdersAsync();
        }
    }
}
