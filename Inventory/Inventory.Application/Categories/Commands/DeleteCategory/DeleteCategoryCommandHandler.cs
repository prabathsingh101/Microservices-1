using Inventory.Application.Common.Interfaces;
using MediatR;

internal sealed class DeleteCategoryCommandHandler
    : IRequestHandler<DeleteCategoryCommand>
{
    private readonly ICategoryRepository _repository;
    private readonly IInventoryDbContext _context;

    public DeleteCategoryCommandHandler(
        ICategoryRepository repository,
        IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(request.Id);

        if (category is null)
            throw new KeyNotFoundException("Category not found");

        // ? BUSINESS RULE
        if (await _repository.HasSubcategoriesAsync(request.Id))
            throw new InvalidOperationException(
                "Cannot delete category with subcategories");

        _repository.Delete(category);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
