using MediatR;

namespace Inventory.Application.SaleOrders.Commands
{
    public record DeleteSaleOrderCommand(int Id) : IRequest<bool>;
}
