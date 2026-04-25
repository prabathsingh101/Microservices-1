using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.Categories.Commands.UpdateCategory;

internal sealed class UpdateCategoryCommandHandler
    : IRequestHandler<UpdateCategoryCommand, Guid>
{
    private readonly ICategoryRepository _repository;
    private readonly IInventoryDbContext _context;

    public UpdateCategoryCommandHandler(
        ICategoryRepository repository,
        IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<Guid> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(request.Id);

        if (category is null)
            throw new KeyNotFoundException("Subcategory not found");

        category.Update(
            request.CategoryName,
            request.CategoryCode,
            request.DefaultGst,
            request.Description,
            request.IsActive,
            request.CompanyId,
            null, // parentCategoryId
            request.BranchId
        );

        await _context.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
