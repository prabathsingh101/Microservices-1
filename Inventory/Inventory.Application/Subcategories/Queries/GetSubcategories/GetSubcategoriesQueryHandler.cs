using MediatR;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Subcategories.DTOs;

namespace Inventory.Application.Subcategories.Queries.GetSubcategories;

public sealed class GetSubcategoriesQueryHandler
    : IRequestHandler<GetSubcategoriesQuery, List<SubcategoryDto>>
{
    private readonly ISubcategoryRepository _repository;

    public GetSubcategoriesQueryHandler(ISubcategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SubcategoryDto>> Handle(
        GetSubcategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _repository.GetAllAsync();

        return list.Select(s => new SubcategoryDto
        {
            Id = s.Id,
            CategoryId = s.CategoryId,
            CategoryName=s.Category.CategoryName,
            SubcategoryCode = s.SubcategoryCode,
            SubcategoryName = s.SubcategoryName,
            DefaultGst = s.DefaultGst,
            Description = s.Description,
            IsActive = s.IsActive
        }).ToList();
    }
}
