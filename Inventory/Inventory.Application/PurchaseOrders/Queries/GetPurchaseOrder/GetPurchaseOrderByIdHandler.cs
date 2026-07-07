using Inventory.Application.Common.Interfaces;
using MediatR;
using Inventory.Application.Clients;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.PurchaseOrders.Queries.GetPurchaseOrder
{
    public class GetPurchaseOrderByIdHandler : IRequestHandler<GetPurchaseOrderByIdQuery, PurchaseOrderDto?>
    {
        private readonly IPurchaseOrderRepository _repository;
        private readonly ISupplierClient _supplierClient;
        private readonly IInventoryDbContext _context;

        public GetPurchaseOrderByIdHandler(IPurchaseOrderRepository repository, ISupplierClient supplierClient, IInventoryDbContext context)
        {
            _repository = repository;
            _supplierClient = supplierClient;
            _context = context;
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
            bool isOffline = false;
            if (searchTerms.Any())
            {
                try
                {
                    paymentStatuses = await _supplierClient.GetGRNPaymentStatusesAsync(searchTerms.Distinct().ToList());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Payment Status Sync Error: {ex.Message}");
                    isOffline = true;
                }
            }

            decimal? supplierBalance = null;
            try
            {
                var balances = await _supplierClient.GetSupplierBalancesAsync(new List<Guid> { po.SupplierId });
                if (balances.ContainsKey(po.SupplierId))
                {
                    supplierBalance = balances[po.SupplierId];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Supplier Balance Sync Error: {ex.Message}");
            }
            
            decimal paidFromPO = 0;
            if (!string.IsNullOrEmpty(po.PoNumber))
            {
                var trimmedPo = po.PoNumber.Trim().ToLower();
                var matchKey = paymentStatuses.Keys.FirstOrDefault(k => k.Trim().ToLower() == trimmedPo);
                if (matchKey != null)
                {
                    paidFromPO = paymentStatuses[matchKey];
                }
            }

            decimal paidFromGRN = 0;
            if (po.GrnHeaders != null)
            {
                foreach (var gh in po.GrnHeaders)
                {
                    if (!string.IsNullOrEmpty(gh.GRNNumber))
                    {
                        var trimmedGrn = gh.GRNNumber.Trim().ToLower();
                        var matchKey = paymentStatuses.Keys.FirstOrDefault(k => k.Trim().ToLower() == trimmedGrn);
                        if (matchKey != null)
                        {
                            paidFromGRN += paymentStatuses[matchKey];
                        }
                    }
                }
            }
            decimal actualPaidAmount = paidFromPO + paidFromGRN;
            // ----------------------------------

            var items = po.Items.Select(i => 
            {
                var totalReturned = _context.PurchaseReturnItems
                    .Where(ri => ri.ProductId == i.ProductId && 
                                 ri.PurchaseReturn.Status != "Cancelled" && 
                                 ri.PurchaseReturn.Status != "Canceled" &&
                                 _context.GRNHeaders.Any(gh => gh.GRNNumber == ri.GrnRef && gh.PurchaseOrderId == po.Id))
                    .Sum(ri => (decimal?)ri.ReturnQty) ?? 0;

                var totalRefunded = _context.PurchaseReturnItems
                    .Where(ri => ri.ProductId == i.ProductId && 
                                 (ri.PurchaseReturn.Status == "Refund" || ri.PurchaseReturn.Status == "Confirmed") &&
                                 _context.GRNHeaders.Any(gh => gh.GRNNumber == ri.GrnRef && gh.PurchaseOrderId == po.Id))
                    .Sum(ri => (decimal?)ri.ReturnQty) ?? 0;

                var grnSummary = _context.GRNDetails
                    .Where(gd => gd.ProductId == i.ProductId && gd.GRNHeader.PurchaseOrderId == po.Id)
                    .Select(gd => new { gd.ReceivedQty, gd.RejectedQty, gd.IsSettled })
                    .ToList();

                var totalAccepted = grnSummary.Sum(s => s.ReceivedQty - s.RejectedQty);
                var totalRejected = grnSummary.Where(s => !s.IsSettled).Sum(s => s.RejectedQty);
                if (totalAccepted < 0) totalAccepted = 0;

                var netAccepted = Math.Max(0, totalAccepted - totalReturned);

                decimal singleUnitValue = i.Qty > 0 ? (i.Total / i.Qty) : 0;
                decimal itemReturnedValue = singleUnitValue * totalReturned;

                return new PurchaseOrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? "N/A",
                    Qty = i.Qty,
                    Unit = i.Unit,
                    Rate = i.Rate,
                    DiscountPercent = i.DiscountPercent,
                    GstPercent = i.GstPercent,
                    TaxAmount = i.TaxAmount,
                    Total = i.Total,
                    ManufacturingDate = i.MfgDate ?? _context.GRNDetails.IgnoreQueryFilters()
                        .Where(gd => gd.ProductId == i.ProductId && gd.GRNHeader.PurchaseOrderId == po.Id && gd.CompanyId == po.CompanyId)
                        .OrderByDescending(gd => gd.Id)
                        .Select(gd => gd.MfgDate)
                        .FirstOrDefault(),
                    ExpiryDate = i.ExpDate ?? _context.GRNDetails.IgnoreQueryFilters()
                        .Where(gd => gd.ProductId == i.ProductId && gd.GRNHeader.PurchaseOrderId == po.Id && gd.CompanyId == po.CompanyId)
                        .OrderByDescending(gd => gd.Id)
                        .Select(gd => gd.ExpDate)
                        .FirstOrDefault(),
                    IsExpiryRequired = i.Product != null ? i.Product.IsExpiryRequired : false,
                    ReceivedQty = i.ReceivedQty - totalReturned,
                    AcceptedQty = netAccepted,
                    RejectedQty = totalRejected,
                    ReturnQty = totalReturned,
                    ReturnAmount = itemReturnedValue,
                    PendingQty = (po.Status == "Closed" || po.Status == "ShortClosed" || po.Status == "Cancelled")
                                 ? 0
                                 : Math.Max(0, i.Qty - netAccepted - totalRefunded)
                };
            }).ToList();

            var hasPendingRefund = items.Any(i => {
                var pendingRefund = _context.PurchaseReturnItems
                    .Where(ri => ri.ProductId == i.ProductId && 
                                 ri.PurchaseReturn.Status == "Confirmed" &&
                                 _context.GRNHeaders.Any(gh => gh.GRNNumber == ri.GrnRef && gh.PurchaseOrderId == po.Id))
                    .Sum(ri => (decimal?)ri.ReturnQty) ?? 0;
                return pendingRefund > 0;
            });

            var poDto = new PurchaseOrderDto
            {
                Id = po.Id,
                PoNumber = po.PoNumber,
                PoDate = po.PoDate,
                SupplierId = po.SupplierId,
                SupplierName = po.SupplierName,
                PriceListId = po.PriceListId,
                ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                Remarks = po.Remarks,
                HasPendingRefund = hasPendingRefund,
                TotalQuantity = po.TotalQuantity,
                TotalTax = po.TotalTax,
                SubTotal= po.SubTotal,
                GrandTotal = po.GrandTotal,
                PaidAmount = actualPaidAmount,
                SupplierBalance = supplierBalance,
                Status = (po.Status == "Cancelled")
                         ? po.Status
                         : (po.Status == "Closed" || po.Status == "ShortClosed")
                             ? (items.Sum(i => i.ReturnQty) > 0 ? "ShortClosed" : "Closed")
                             : po.Status,
                BranchId = po.BranchId,
                Items = items,
                TotalReturnedAmount = items.Sum(i => i.ReturnAmount)
            };
            if (isOffline)
            {
                poDto.PaymentStatus = "Offline";
            }
            return poDto;
        }
    }
}
