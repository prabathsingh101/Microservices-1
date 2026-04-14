using Inventory.Application.Subcategories.DTOs;
using MediatR;

public sealed record GetSubcategoriesByCategoryQuery(Guid CategoryId)
    : IRequest<List<SubcategoryDto>>;
