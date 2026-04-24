namespace Inventory.Application.Locations.Warehouses.DTOs;

public record WarehouseDto(
    Guid Id,
    string Name,
    string? City,
    string? Description,
    bool IsActive,
    string? BranchId
);

public record CreateWarehouseDto(
    string Name,
    string? City,
    string? Description,
    bool IsActive
);

public record UpdateWarehouseDto(
    Guid Id,
    string Name,
    string? City,
    string? Description,
    bool IsActive
);
