using MediatR;
using System;

namespace Inventory.Application.SaleOrders.SaleReturn.Command
{
    public class CancelSaleReturnCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string? Reason { get; set; }

        public CancelSaleReturnCommand(Guid id, string? reason)
        {
            Id = id;
            Reason = reason;
        }
    }
}
