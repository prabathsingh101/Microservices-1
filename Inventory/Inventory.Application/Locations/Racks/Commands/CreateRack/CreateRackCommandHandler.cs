using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using MediatR;

namespace Inventory.Application.Locations.Racks.Commands.CreateRack;

public sealed class CreateRackCommandHandler : IRequestHandler<CreateRackCommand, Guid>
{
    private readonly IRackRepository _repository;
    private readonly IInventoryDbContext _context;

    public CreateRackCommandHandler(IRackRepository repository, IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<Guid> Handle(CreateRackCommand request, CancellationToken cancellationToken)
    {
        var rack = new Rack(
            request.WarehouseId,
            request.Name,
            request.Description,
            request.IsActive,
            request.CompanyId
        );

        await _repository.AddAsync(rack);
        await _context.SaveChangesAsync(cancellationToken);

        return rack.Id;
    }
}
