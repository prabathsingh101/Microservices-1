using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseOrders.Queries.GetPurchaseOrder
{
    public record GetPurchaseOrderByIdQuery(Guid Id) : IRequest<PurchaseOrderDto?>;
}
