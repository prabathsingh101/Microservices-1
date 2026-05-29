using Inventory.Application.Common.Interfaces;
using MediatR;
using Inventory.Application.Clients;
using System.Linq;

namespace Inventory.Application.PurchaseOrders.Queries.GetPurchaseOrder
{
    public class GetPurchaseOrderByIdHandler : IRequestHandler<GetPurchaseOrderByIdQuery, PurchaseOrderDto?>
    {
        private readonly IPurchaseOrderRepository _repository;
        private readonly ISupplierClient _supplierClient;

        public GetPurchaseOrderByIdHandler(IPurchaseOrderRepository repository, ISupplierClient supplierClient)
        {
            _repository = repository;
            _supplierClient = supplierClient;
        }

        public async Task<PurchaseOrderDto?> Handle(GetPurchaseOrderByIdQuery request, CancellationToken cancellationToken)
        {
            // Ensure repository includes the Product navigation property if needed
            var po = await _repository.GetByIdWithItemsAsync(request.Id, cancellationToken);

            if (po == null) return null;

            // --- CROSS MODULE PAYMENT CHECK ---
            var searchTerms = new List<string>();
            if (!string.IsNullOrEmpty(po.PoNumber)) searchTerms.Add(po.PoNumber);
            if (po.GrnHeaders != null)
            {
                foreach (var gh in po.GrnHeaders)
                {
                    if (!string.IsNullOrEmpty(gh.GRNNumber)) searchTerms.Add(gh.GRNNumber);
                }
            }

            var paymentStatuses = new Dictionary<string, decimal>();
            if (searchTerms.Any())
            {
                try
                {
                    paymentStatuses = await _supplierClient.GetGRNPaymentStatusesAsync(searchTerms.Distinct().ToList());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Payment Status Sync Error: {ex.Message}");
                }
            }
            
            decimal paidFromPO = (!string.IsNullOrEmpty(po.PoNumber) && paymentStatuses.ContainsKey(po.PoNumber)) ? paymentStatuses[po.PoNumber] : 0;
            decimal paidFromGRN = 0;
            if (po.GrnHeaders != null)
            {
                foreach (var gh in po.GrnHeaders)
                {
                    if (!string.IsNullOrEmpty(gh.GRNNumber) && paymentStatuses.ContainsKey(gh.GRNNumber))
                    {
                        paidFromGRN += paymentStatuses[gh.GRNNumber];
                    }
                }
            }
            decimal actualPaidAmount = paidFromPO + paidFromGRN;
            // ----------------------------------

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
                PaidAmount = actualPaidAmount,
                Status = po.Status,
                // .Select mapping ensures each item is converted properly
                Items = po.Items.Select(i => new PurchaseOrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId, // Ensure this property in DB is not null
                    ProductName = i.Product?.Name ?? "N/A",
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
