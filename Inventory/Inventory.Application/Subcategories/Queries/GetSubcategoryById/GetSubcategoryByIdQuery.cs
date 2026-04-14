using MediatR;
using Inventory.Application.Subcategories.DTOs;

namespace Inventory.Application.Subcategories.Queries.GetSubcategoryById;

public sealed record GetSubcategoryByIdQuery(Guid Id)
    : IRequest<SubcategoryDto?>;
