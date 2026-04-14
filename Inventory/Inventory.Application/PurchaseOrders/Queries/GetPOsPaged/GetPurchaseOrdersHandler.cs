using Inventory.Application.Common.Interfaces;
using MediatR;

public class GetPurchaseOrdersHandler : IRequestHandler<GetPurchaseOrdersQuery, PagedResponse<PurchaseOrderDto>>
{
    private readonly IPurchaseOrderRepository _repo;

    public GetPurchaseOrdersHandler(IPurchaseOrderRepository repo) => _repo = repo;

    public async Task<PagedResponse<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken ct)
    {
        var (orders, total) = await _repo.GetPagedOrdersAsync(
            request.PageIndex, request.PageSize, request.SortField, request.SortOrder, request.Filter);

        // Manual Mapping
        var dtos = orders.Select(x => PurchaseOrderDto.FromEntity(x)).ToList();

        return new PagedResponse<PurchaseOrderDto>(dtos, total, request.PageIndex, request.PageSize);
    }
}
