using MediatR;

namespace Inventory.Application.SaleOrders.Commands
{
    public record DeleteSaleOrderCommand(Guid Id, string? Reason = null) : IRequest<bool>;
}
