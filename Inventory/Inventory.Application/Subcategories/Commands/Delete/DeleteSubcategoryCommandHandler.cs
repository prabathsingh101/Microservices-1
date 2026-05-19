using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.Subcategories.Commands.Delete;

internal sealed class DeleteSubcategoryCommandHandler
    : IRequestHandler<DeleteSubcategoryCommand, Guid>
{
    private readonly ISubcategoryRepository _repository;
    private readonly IInventoryDbContext _context;

    public DeleteSubcategoryCommandHandler(
        ISubcategoryRepository repository,
        IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<Guid> Handle(
        DeleteSubcategoryCommand request,
        CancellationToken cancellationToken)
    {
        var subcategory = await _repository.GetByIdAsync(request.Id);

        if (subcategory is null)
            throw new KeyNotFoundException("Subcategory not found");

        await _repository.DeleteAsync(subcategory);

        await _context.SaveChangesAsync(cancellationToken);

        return request.Id;
    }
}
