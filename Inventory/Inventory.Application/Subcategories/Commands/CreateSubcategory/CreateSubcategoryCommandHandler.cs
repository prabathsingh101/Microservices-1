using MediatR;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;

namespace Inventory.Application.Subcategories.Commands.CreateSubcategory;

public sealed class CreateSubcategoryCommandHandler
    : IRequestHandler<CreateSubcategoryCommand, Guid>
{
    private readonly ISubcategoryRepository _repository;

    private readonly IInventoryDbContext _context;

    public CreateSubcategoryCommandHandler(ISubcategoryRepository repository, IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<Guid> Handle(
        CreateSubcategoryCommand request,
        CancellationToken cancellationToken)
    {
        var subcategory = new Subcategory(
            request.categoryid,
            request.subcategorycode,
            request.subcategoryname,
            request.defaultgst,
            request.description,
            request.isactive,
            request.CompanyId
        );

        await _repository.AddAsync(subcategory);

        await _context.SaveChangesAsync();

        return subcategory.Id;
    }
}
