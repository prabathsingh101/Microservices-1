using MediatR;
using Inventory.Application.Subcategories.DTOs;

namespace Inventory.Application.Subcategories.Queries.GetSubcategories;

public sealed record GetSubcategoriesQuery
    : IRequest<List<SubcategoryDto>>;
