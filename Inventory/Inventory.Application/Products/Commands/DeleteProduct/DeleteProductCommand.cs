using MediatR;

namespace Inventory.Application.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id)
    : IRequest<Guid>;
