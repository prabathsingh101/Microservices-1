using Inventory.Application.SaleOrders.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.SaleOrders.Commands
{
    public record CreateSaleOrderCommand(CreateSaleOrderDto OrderDto) : IRequest<object>;
}
