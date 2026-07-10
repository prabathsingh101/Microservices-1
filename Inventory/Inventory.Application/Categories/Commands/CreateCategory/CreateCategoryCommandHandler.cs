using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using MediatR;

namespace Inventory.Application.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler
    : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly ICategoryRepository _repository;
    private readonly IInventoryDbContext _context;

    public CreateCategoryCommandHandler(ICategoryRepository repository,IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<Guid> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = new Category(
            request.CategoryName,
            request.CategoryCode,
            request.DefaultGst,
            request.Description,
            request.IsActive,
            request.CompanyId,
            null, // parentCategoryId
            request.BranchId,
            request.IndustryType
        );

        await _repository.AddAsync(category);

        await _context.SaveChangesAsync();

        return category.Id;
    }
}
