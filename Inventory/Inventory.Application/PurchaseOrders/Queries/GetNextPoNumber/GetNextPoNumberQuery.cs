using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseOrders.Queries.GetNextPoNumber
{
    public record GetNextPoNumberQuery(bool IsQuick = false) : IRequest<string>;
}
