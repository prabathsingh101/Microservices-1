using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.DTOs;
using MediatR;

internal sealed class GetProductLookupsQueryHandler
    : IRequestHandler<GetProductLookupsQuery, ProductLookupDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISubcategoryRepository _subcategoryRepository;

    public GetProductLookupsQueryHandler(
        ICategoryRepository categoryRepository,
        ISubcategoryRepository subcategoryRepository)
    {
        _categoryRepository = categoryRepository;
        _subcategoryRepository = subcategoryRepository;
    }

    public async Task<ProductLookupDto> Handle(
        GetProductLookupsQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetAllAsync();
        var subcategories = await _subcategoryRepository.GetAllAsync();

        return new ProductLookupDto
        {
            Categories = categories.Select(c => new CategoryLookupDto
            {
                Id = c.Id,
                Name = c.CategoryName,
                categoryCode = c.CategoryCode,
                IndustryType = c.IndustryType
            }).ToList(),

            Subcategories = subcategories.Select(s => new SubcategoryLookupDto
            {
                Id = s.Id,
                CategoryId = s.CategoryId,
                Name = s.SubcategoryName               

            }).ToList()
        };
    }
}
