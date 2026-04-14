using Inventory.Application.PurchaseOrders.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseOrders.Queries.GetPurchaseOrder
{
    public record GetPendingPOQuery() : IRequest<IEnumerable<PendingPODto>>;
}
