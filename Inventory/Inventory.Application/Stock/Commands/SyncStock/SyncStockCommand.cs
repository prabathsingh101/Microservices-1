using MediatR;

namespace Inventory.Application.Stock.Commands
{
    public class SyncStockCommand : IRequest<bool>
    {
    }
}
