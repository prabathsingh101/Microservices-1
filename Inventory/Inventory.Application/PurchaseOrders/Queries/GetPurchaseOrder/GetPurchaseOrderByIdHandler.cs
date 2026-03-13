using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.PurchaseOrders.Queries.GetPurchaseOrder
{
    public class GetPurchaseOrderByIdHandler : IRequestHandler<GetPurchaseOrderByIdQuery, PurchaseOrderDto?>
    {
        private readonly IPurchaseOrderRepository _repository;

        public GetPurchaseOrderByIdHandler(IPurchaseOrderRepository repository)
        {
            _repository = repository;
        }

        public async Task<PurchaseOrderDto?> Handle(GetPurchaseOrderByIdQuery request, CancellationToken cancellationToken)
        {
            // Ensure repository includes the Product navigation property if needed
            var po = await _repository.GetByIdWithItemsAsync(request.Id, cancellationToken);

            if (po == null) return null;

            return new PurchaseOrderDto
            {
                Id = po.Id,
                PoNumber = po.PoNumber,
                PoDate = po.PoDate,
                SupplierId = po.SupplierId,
                SupplierName = po.SupplierName,
                PriceListId = po.PriceListId,
                ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                Remarks = po.Remarks,
                TotalQuantity = po.TotalQuantity,
                TotalTax = po.TotalTax,
                SubTotal= po.SubTotal,
                GrandTotal = po.GrandTotal,
                Status = po.Status,
                // .Select mapping ensures each item is converted properly
                Items = po.Items.Select(i => new PurchaseOrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId, // Ensure this property in DB is not null
                    ProductName = i.Product.Name,
                    Qty = i.Qty,
                    Unit = i.Unit,
                    Rate = i.Rate,
                    DiscountPercent = i.DiscountPercent,
                    GstPercent = i.GstPercent,
                    TaxAmount = i.TaxAmount,
                    Total = i.Total,
                    ManufacturingDate = i.MfgDate,
                    ExpiryDate = i.ExpDate,
                    IsExpiryRequired = i.Product != null ? i.Product.IsExpiryRequired : false
                }).ToList()
            };
        }
    }
}