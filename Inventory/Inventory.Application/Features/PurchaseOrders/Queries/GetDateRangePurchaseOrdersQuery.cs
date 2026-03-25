using Inventory.Application.PurchaseOrders.DTOs;
using MediatR;

namespace Inventory.Application.Features.PurchaseOrders.Queries
{
    public class GetDateRangePurchaseOrdersQuery : IRequest<PurchaseOrderPagedResponse>
    {
        public GetPurchaseOrdersRequest Request { get; set; }
        public GetDateRangePurchaseOrdersQuery(GetPurchaseOrdersRequest request) => Request = request;
    }
}
