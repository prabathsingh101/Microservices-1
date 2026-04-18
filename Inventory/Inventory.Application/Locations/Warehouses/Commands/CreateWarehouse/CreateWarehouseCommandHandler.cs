using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using MediatR;

namespace Inventory.Application.Locations.Warehouses.Commands.CreateWarehouse;

public sealed class CreateWarehouseCommandHandler : IRequestHandler<CreateWarehouseCommand, Guid>
{
    private readonly IWarehouseRepository _repository;
    private readonly IInventoryDbContext _context;

    public CreateWarehouseCommandHandler(IWarehouseRepository repository, IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<Guid> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = new Warehouse(
            request.Name,
            request.City,
            request.Description,
            request.IsActive,
            request.CompanyId
        );

        await _repository.AddAsync(warehouse);
        await _context.SaveChangesAsync(cancellationToken);

        return warehouse.Id;
    }
}
