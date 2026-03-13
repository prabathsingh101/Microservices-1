using MediatR;
using System;

namespace Inventory.Application.Stock.Commands.RejectStock
{
    public record RejectStockCommand(
        Guid ProductId,
        Guid WarehouseId,
        Guid RackId,
        decimal Quantity,
        DateTime? ExpiryDate,
        string Reason = "Expired Stock Removal"
    ) : IRequest<bool>;
}
