using MediatR;

namespace Inventory.Application.Products.Queries.GetProducts;

public sealed record GetProductsQuery
    : IRequest<List<ProductDto>>;
