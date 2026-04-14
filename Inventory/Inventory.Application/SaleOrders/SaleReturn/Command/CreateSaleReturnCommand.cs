using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.SaleOrders.SaleReturn.Command
{
    public record CreateSaleReturnCommand(CreateSaleReturnDto Dto) : IRequest<bool>;
}
