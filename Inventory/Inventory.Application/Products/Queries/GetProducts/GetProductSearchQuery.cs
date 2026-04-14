using Inventory.Application.Products.DTOs;
using MediatR;

public record GetProductSearchQuery(string Term) : IRequest<List<ProductSearchResponseDto>>;
