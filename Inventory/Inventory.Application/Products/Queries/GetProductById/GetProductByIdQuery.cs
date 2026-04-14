using MediatR;

namespace Inventory.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id)
    : IRequest<ProductDto?>;
