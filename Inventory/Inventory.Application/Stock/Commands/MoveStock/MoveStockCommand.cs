using MediatR;
using System;

namespace Inventory.Application.Stock.Commands.MoveStock
{
    public record MoveStockCommand : IRequest<bool>
    {
        public Guid ProductId { get; init; }
        public Guid SourceWarehouseId { get; init; }
        public Guid SourceRackId { get; init; }
        public Guid TargetWarehouseId { get; init; }
        public Guid TargetRackId { get; init; }
        public decimal Quantity { get; init; }
        public DateTime? ExpiryDate { get; init; }
        public string? BranchId { get; init; }
    }
}
