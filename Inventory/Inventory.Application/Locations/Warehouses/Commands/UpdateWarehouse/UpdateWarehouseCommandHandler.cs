using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.Locations.Warehouses.Commands.UpdateWarehouse;

public sealed class UpdateWarehouseCommandHandler : IRequestHandler<UpdateWarehouseCommand, Unit>
{
    private readonly IWarehouseRepository _repository;
    private readonly IInventoryDbContext _context;

    public UpdateWarehouseCommandHandler(IWarehouseRepository repository, IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<Unit> Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await _repository.GetByIdAsync(request.Id);

        if (warehouse == null)
        {
            throw new Exception("Warehouse not found");
        }

        warehouse.Update(request.Name, request.City, request.Description, request.IsActive, request.CompanyId);

        await _repository.UpdateAsync(warehouse);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
