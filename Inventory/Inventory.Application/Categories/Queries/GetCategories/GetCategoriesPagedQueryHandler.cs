using Inventory.Application.Categories.DTOs;
using Inventory.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

internal sealed class GetCategoriesPagedQueryHandler
    : IRequestHandler<GetCategoriesPagedQuery, GridResponse<CategoryDto>>
{
    private readonly ICategoryRepository _repository;

    public GetCategoriesPagedQueryHandler(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<GridResponse<CategoryDto>> Handle(
           GetCategoriesPagedQuery request,
           CancellationToken cancellationToken)
    {
        var query = _repository
            .Query()
            .AsQueryable();

        // ============================
        // ?? GLOBAL SEARCH
        // ============================
        if (!string.IsNullOrWhiteSpace(request.Query.Search))
        {
            var search = $"%{request.Query.Search.Trim()}%";

            query = query.Where(x =>
                EF.Functions.Like(x.CategoryName, search) ||
                EF.Functions.Like(x.CategoryCode, search) ||
                EF.Functions.Like(x.Description, search)
            );
        }

        // ============================
        // ?? COLUMN FILTERS (FIXED)
        // ============================
        if (request.Query.Filters != null && request.Query.Filters.Any())
        {
            foreach (var filter in request.Query.Filters)
            {
                var value = filter.Value?.Trim();
                if (string.IsNullOrEmpty(value))
                    continue;

                var likeValue = $"%{value}%";

                query = filter.Key switch
                {
                    "categoryName" =>
                        query.Where(x => EF.Functions.Like(x.CategoryName, likeValue)),

                    "categoryCode" =>
                        query.Where(x => EF.Functions.Like(x.CategoryCode, likeValue)),                   

                    "description" =>
                        query.Where(x => x.Description.ToString().Contains(value)),

                    "isActive" =>
                        query.Where(x => x.IsActive == (value == "true" || value == "yes")),

                    _ => query
                };
            }
        }

        // ============================
        // ?? SORTING
        // ============================
        query = request.Query.SortBy switch
        {
            "categoryName" =>
                request.Query.SortDirection == "asc"
                    ? query.OrderBy(x => x.CategoryName)
                    : query.OrderByDescending(x => x.CategoryName),

            "categoryCode" =>
                request.Query.SortDirection == "asc"
                    ? query.OrderBy(x => x.CategoryCode)
                    : query.OrderByDescending(x => x.CategoryCode),
           

            "description" =>
                request.Query.SortDirection == "asc"
                    ? query.OrderBy(x => x.Description)
                    : query.OrderByDescending(x => x.Description),

            "createdOn" =>
                request.Query.SortDirection == "asc"
                    ? query.OrderBy(x => x.CreatedOn)
                    : query.OrderByDescending(x => x.CreatedOn),

            _ => query.OrderByDescending(x => x.CreatedOn)
        };

        // ============================
        // ?? COUNT
        // ============================
        var totalCount = await query.CountAsync(cancellationToken);

        // ============================
        // ?? PAGING + DTO
        // ============================
        var items = await query
            .Skip((request.Query.PageNumber - 1) * request.Query.PageSize)
            .Take(request.Query.PageSize)
            .Select(x => new CategoryDto
            {
                id = x.Id,
                categoryName = x.CategoryName,               
                categoryCode = x.CategoryCode,
                defaultGst = x.DefaultGst,
                isActive = x.IsActive,
                description = x.Description,
          
            })
            .ToListAsync(cancellationToken);

        return new GridResponse<CategoryDto>(items, totalCount);
    }
}
