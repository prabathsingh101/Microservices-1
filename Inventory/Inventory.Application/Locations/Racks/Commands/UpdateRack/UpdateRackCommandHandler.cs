using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.Locations.Racks.Commands.UpdateRack;

public sealed class UpdateRackCommandHandler : IRequestHandler<UpdateRackCommand, Unit>
{
    private readonly IRackRepository _repository;
    private readonly IInventoryDbContext _context;

    public UpdateRackCommandHandler(IRackRepository repository, IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<Unit> Handle(UpdateRackCommand request, CancellationToken cancellationToken)
    {
        var rack = await _repository.GetByIdAsync(request.Id);

        if (rack == null)
        {
            throw new Exception("Rack not found");
        }

        rack.Update(request.WarehouseId, request.Name, request.Description, request.IsActive, request.CompanyId, request.BranchId);

        await _repository.UpdateAsync(rack);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
