using MediatR;
using Inventory.Application.Categories.DTOs;

namespace Inventory.Application.Categories.Queries.GetCategoryById;

public sealed record GetCategoryByIdQuery(Guid Id)
    : IRequest<CategoryDto?>;
