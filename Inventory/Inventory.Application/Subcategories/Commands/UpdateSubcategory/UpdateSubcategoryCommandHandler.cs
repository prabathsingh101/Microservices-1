using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.Subcategories.Commands.UpdateSubcategory;

internal sealed class UpdateSubcategoryCommandHandler
    : IRequestHandler<UpdateSubcategoryCommand, Guid>
{
    private readonly ISubcategoryRepository _repository;
    private readonly IInventoryDbContext _context;

    public UpdateSubcategoryCommandHandler(
        ISubcategoryRepository repository,
        IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<Guid> Handle(
        UpdateSubcategoryCommand request,
        CancellationToken cancellationToken)
    {
        var subcategory = await _repository.GetByIdAsync(request.Id);

        if (subcategory is null)
            throw new KeyNotFoundException("Subcategory not found");

        subcategory.Update(
            request.Code,
            request.Name,
            request.CategoryId,                      
            request.DefaultGst,
            request.Description,
            request.IsActive,
            request.CompanyId,
            request.BranchId
        );

        await _context.SaveChangesAsync(cancellationToken);

        return subcategory.Id;
    }
}
