using Inventory.Application.Common.Interfaces;
using Inventory.Application.Subcategories.DTOs;
using MediatR;

namespace Inventory.Application.Subcategories.Queries.GetSubcategoriesByCategory;

internal sealed class GetSubcategoriesByCategoryQueryHandler
    : IRequestHandler<GetSubcategoriesByCategoryQuery, List<SubcategoryDto>>
{
    private readonly ISubcategoryRepository _repository;

    public GetSubcategoriesByCategoryQueryHandler(
        ISubcategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SubcategoryDto>> Handle(
        GetSubcategoriesByCategoryQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _repository.GetByCategoryIdAsync(request.CategoryId);

        return list.Select(s => new SubcategoryDto
        {
            Id = s.Id,
            CategoryId = s.CategoryId,
            CategoryName = s.Category.CategoryName,
            SubcategoryCode = s.SubcategoryCode,
            SubcategoryName = s.SubcategoryName,
            DefaultGst = s.DefaultGst,
            Description = s.Description,
            IsActive = s.IsActive
        }).ToList();
    }
}
