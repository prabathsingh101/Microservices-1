using Inventory.Application.Categories.DTOs;
using MediatR;

namespace Inventory.Application.Categories.Queries.GetCategoryById;

public sealed class GetCategoryByIdQueryHandler
    : IRequestHandler<GetCategoryByIdQuery, CategoryDto?>
{
    private readonly ICategoryRepository _repository;

    public GetCategoryByIdQueryHandler(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<CategoryDto?> Handle(
        GetCategoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        var c = await _repository.GetByIdAsync(request.Id);
        if (c is null) return null;

        return new CategoryDto
        {
            id = c.Id,
            categoryName = c.CategoryName,
            categoryCode = c.CategoryCode,
            defaultGst = c.DefaultGst,
            description = c.Description,
            isActive = c.IsActive
        };
    }
}
