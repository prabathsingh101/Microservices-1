using MediatR;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Subcategories.DTOs;

namespace Inventory.Application.Subcategories.Queries.GetSubcategoryById;

public sealed class GetSubcategoryByIdQueryHandler
    : IRequestHandler<GetSubcategoryByIdQuery, SubcategoryDto?>
{
    private readonly ISubcategoryRepository _repository;

    public GetSubcategoryByIdQueryHandler(ISubcategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<SubcategoryDto?> Handle(
        GetSubcategoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        var s = await _repository.GetByIdAsync(request.Id);
        if (s is null) return null;

        return new SubcategoryDto
        {
            Id = s.Id,
            CategoryId = s.CategoryId,
            SubcategoryCode = s.SubcategoryCode,
            SubcategoryName = s.SubcategoryName,
            DefaultGst = s.DefaultGst,
            Description = s.Description,
            IsActive = s.IsActive
        };
    }
}
