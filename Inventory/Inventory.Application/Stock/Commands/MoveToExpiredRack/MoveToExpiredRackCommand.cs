using MediatR;
using System;

namespace Inventory.Application.Stock.Commands.MoveToExpiredRack
{
    public record MoveToExpiredRackCommand : IRequest<bool>
    {
        public Guid ProductId { get; init; }
        public Guid SourceWarehouseId { get; init; }
        public Guid SourceRackId { get; init; }
        public decimal Quantity { get; init; }
        public DateTime? ExpiryDate { get; init; }
    }
}
