using MediatR;

namespace Inventory.Application.Locations.Racks.Commands.CreateRack;

public record CreateRackCommand(
    Guid WarehouseId,
    string Name,
    string? Description,
    bool IsActive,
    Guid CompanyId,
    string? BranchId = null
) : IRequest<Guid>;
