using MediatR;

namespace Inventory.Application.Locations.Warehouses.Commands.UpdateWarehouse;

public record UpdateWarehouseCommand(
    Guid Id,
    string Name,
    string? City,
    string? Description,
    bool IsActive,
    Guid CompanyId,
    string? BranchId = null
) : IRequest<Unit>;
