using Inventory.Application.Common.Interfaces;
using Inventory.Application.PurchaseOrders.DTOs;
using MediatR;

public class GetPOItemsForGRNHandler : IRequestHandler<GetPOItemsForGRNQuery, IEnumerable<POItemForGRNDto>>
{
    private readonly IPurchaseOrderRepository _repo;

    public GetPOItemsForGRNHandler(IPurchaseOrderRepository repo) => _repo = repo;

    public async Task<IEnumerable<POItemForGRNDto>> Handle(GetPOItemsForGRNQuery request, CancellationToken ct)
    {
        return await _repo.GetPOItemsForGRNAsync(request.PoId);
    }
}
