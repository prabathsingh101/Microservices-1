using Inventory.Application.Common.Interfaces;
using Inventory.Application.Locations.Warehouses.DTOs;
using MediatR;

namespace Inventory.Application.Locations.Warehouses.Queries.GetWarehouses;

public sealed class GetWarehousesQueryHandler : IRequestHandler<GetWarehousesQuery, List<WarehouseDto>>
{
    private readonly IWarehouseRepository _repository;

    public GetWarehousesQueryHandler(IWarehouseRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        var warehouses = await _repository.GetAllAsync();

        return warehouses.Select(w => new WarehouseDto(
            w.Id,
            w.Name,
            w.City,
            w.Description,
            w.IsActive,
            w.BranchId
        )).ToList();
    }
}
