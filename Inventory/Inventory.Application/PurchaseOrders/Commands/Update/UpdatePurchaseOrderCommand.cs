using Inventory.Application.PurchaseOrders.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PurchaseOrders.Commands.Update
{
    public record UpdatePurchaseOrderCommand(UpdatePurchaseOrderDto Dto) : IRequest<bool>;
}
