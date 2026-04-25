using Inventory.Application.Subcategories.DTOs;
using MediatR;

namespace Inventory.Application.Subcategories.Queries.GetSubcategoriesByCategory;

public sealed record GetSubcategoriesByCategoryQuery(Guid CategoryId)
    : IRequest<List<SubcategoryDto>>;
