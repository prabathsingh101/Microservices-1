using Inventory.Application.Products.DTOs;
using MediatR;

public sealed record GetProductLookupsQuery
    : IRequest<ProductLookupDto>;
