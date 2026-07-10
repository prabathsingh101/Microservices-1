using AutoMapper;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Features.PurchaseOrders.Queries;
using Inventory.Application.PurchaseOrders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Inventory.Application.Clients;
using System.Linq;

namespace Inventory.Application.Features.PurchaseOrders.Handlers
{
    public class GetDateRangePurchaseOrdersQueryHandler : IRequestHandler<GetDateRangePurchaseOrdersQuery, PurchaseOrderPagedResponse>
    {
        private readonly IPurchaseOrderRepository _repo;
        private readonly IInventoryDbContext _context;
        private readonly ISupplierClient _supplierClient;

        public GetDateRangePurchaseOrdersQueryHandler(
            IPurchaseOrderRepository repo, 
            IInventoryDbContext context,
            ISupplierClient supplierClient)
        {
            _repo = repo;
            _context = context;
            _supplierClient = supplierClient;
        }

        public async Task<PurchaseOrderPagedResponse> Handle(GetDateRangePurchaseOrdersQuery query, CancellationToken ct)
        {
            // 1. Fetch PO Data with stats
            var result = await _repo.GetDateRangePagedOrdersAsync(query.Request);

            // Fetch all product IDs from the current page to check for purges
            var productIds = result.Data.SelectMany(po => po.Items).Select(item => item.ProductId).Distinct().ToList();
            var purgedProductIds = await _context.InventoryTransactions
                .Where(tx => tx.TransactionType == "StockPurge-OUT" && productIds.Contains(tx.ProductId))
                .Select(tx => tx.ProductId)
                .Distinct()
                .ToListAsync(ct);

            // --- CROSS MODULE PAYMENT CHECK ---
            var searchTerms = new List<string>();
            foreach (var po in result.Data)
            {
                if (!string.IsNullOrEmpty(po.PoNumber)) searchTerms.Add(po.PoNumber);
                if (po.GrnHeaders != null)
                {
                    foreach (var gh in po.GrnHeaders)
                    {
                        if (!string.IsNullOrEmpty(gh.GRNNumber)) searchTerms.Add(gh.GRNNumber);
                    }
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

            var supplierBalances = new Dictionary<Guid, decimal>();
            var supplierIds = result.Data.Select(po => po.SupplierId).Distinct().ToList();
            if (supplierIds.Any())
            {
                try
                {
                    supplierBalances = await _supplierClient.GetSupplierBalancesAsync(supplierIds);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Supplier Balance Sync Error: {ex.Message}");
                }
            }
            // ----------------------------------

            // 2. Mapping with Net Quantity Logic
            var dtos = result.Data.Select(x => {
                var items = x.Items.Select(item => {
                    // Fetch all GRN Details for this specific PO Item
                    var grnSummary = _context.GRNDetails
                        .Where(gd => gd.ProductId == item.ProductId && gd.GRNHeader.PurchaseOrderId == x.Id)
                        .Select(gd => new { gd.ReceivedQty, gd.RejectedQty, gd.IsSettled })
                        .ToList();

                    // Fetch total returned quantity for this specific PO item
                    var totalReturned = _context.PurchaseReturnItems
                        .Where(ri => ri.ProductId == item.ProductId && 
                                     ri.PurchaseReturn.Status != "Cancelled" && 
                                     ri.PurchaseReturn.Status != "Canceled" &&
                                     _context.GRNHeaders.Any(gh => gh.GRNNumber == ri.GrnRef && gh.PurchaseOrderId == x.Id))
                        .Sum(ri => (decimal?)ri.ReturnQty) ?? 0;

                    // Fetch total refunded quantity for this specific PO item
                    var totalRefunded = _context.PurchaseReturnItems
                        .Where(ri => ri.ProductId == item.ProductId && 
                                     (ri.PurchaseReturn.Status == "Refund" || ri.PurchaseReturn.Status == "Confirmed") &&
                                     _context.GRNHeaders.Any(gh => gh.GRNNumber == ri.GrnRef && gh.PurchaseOrderId == x.Id))
                        .Sum(ri => (decimal?)ri.ReturnQty) ?? 0;

                    // Dynamic calculation: Accepted = Received - Rejected (Returns are tracked separately and shouldn't reduce accepted if they were already rejected)
                    var totalAccepted = grnSummary.Sum(s => s.ReceivedQty - s.RejectedQty);
                    var totalRejected = grnSummary.Where(s => !s.IsSettled).Sum(s => s.RejectedQty);
                    if (totalAccepted < 0) totalAccepted = 0;

                    var netAccepted = Math.Max(0, totalAccepted - totalReturned);

                    var isAlreadyPurged = grnSummary.Any() && grnSummary.All(s => s.ReceivedQty == 0 && s.RejectedQty == 0) && purgedProductIds.Contains(item.ProductId);

                    decimal singleUnitValue = item.Qty > 0 ? (item.Total / item.Qty) : 0;
                    decimal itemReturnedValue = singleUnitValue * totalReturned;

                    return new PurchaseOrderItemDto
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        Qty = item.Qty, 
                        Unit = item.Unit,
                        Rate = item.Rate,
                        Total = item.Total,
                        TaxAmount = item.TaxAmount,
                        DiscountPercent = item.DiscountPercent,
                        GstPercent = item.GstPercent,
                        ProductName = item.Product != null ? item.Product.Name : "N/A",
                        ProductVariantId = item.ProductVariantId,
                        Color = item.ProductVariant != null ? item.ProductVariant.Color : null,
                        Size = item.ProductVariant != null ? item.ProductVariant.Size : null,

                        // Use the field from PurchaseOrderItems table (which is net-updated by repo) minus returns
                        ReceivedQty = item.ReceivedQty - totalReturned,
                        AcceptedQty = netAccepted,
                        RejectedQty = totalRejected,
                        ReturnQty = totalReturned,
                        ReturnAmount = itemReturnedValue,

                        // Pending = (Ordered - NetAccepted - Refunded) (0 if Closed or Cancelled)
                        PendingQty = (x.Status == "Closed" || x.Status == "ShortClosed" || x.Status == "Cancelled" || x.Status == "Fully Returned")
                                     ? 0
                                     : Math.Max(0, item.Qty - netAccepted - totalRefunded),
                        ManufacturingDate = item.MfgDate ?? _context.GRNDetails.IgnoreQueryFilters()
                            .Where(gd => gd.ProductId == item.ProductId && gd.GRNHeader.PurchaseOrderId == x.Id && gd.CompanyId == x.CompanyId)
                            .OrderByDescending(gd => gd.Id)
                            .Select(gd => gd.MfgDate)
                            .FirstOrDefault(),
                        ExpiryDate = item.ExpDate ?? _context.GRNDetails.IgnoreQueryFilters()
                            .Where(gd => gd.ProductId == item.ProductId && gd.GRNHeader.PurchaseOrderId == x.Id && gd.CompanyId == x.CompanyId)
                            .OrderByDescending(gd => gd.Id)
                            .Select(gd => gd.ExpDate)
                            .FirstOrDefault(),
                        IsExpiryRequired = item.Product != null ? item.Product.IsExpiryRequired : false,
                        WarehouseName = _context.GRNDetails.IgnoreQueryFilters()
                            .Where(gd => gd.ProductId == item.ProductId && gd.GRNHeader.PurchaseOrderId == x.Id && gd.CompanyId == x.CompanyId)
                            .OrderByDescending(gd => gd.Id)
                            .Select(gd => _context.Warehouses.IgnoreQueryFilters().Where(w => w.Id == gd.WarehouseId && w.CompanyId == x.CompanyId).Select(w => w.Name).FirstOrDefault())
                            .FirstOrDefault(),
                        RackName = _context.GRNDetails.IgnoreQueryFilters()
                            .Where(gd => gd.ProductId == item.ProductId && gd.GRNHeader.PurchaseOrderId == x.Id && gd.CompanyId == x.CompanyId)
                            .OrderByDescending(gd => gd.Id)
                            .Select(gd => _context.Racks.IgnoreQueryFilters().Where(r => r.Id == gd.RackId && r.CompanyId == x.CompanyId).Select(r => r.Name).FirstOrDefault())
                            .FirstOrDefault(),
                        IsAlreadyPurged = isAlreadyPurged
                    };
                }).ToList();

                var grnNumber = x.GrnHeaders?.FirstOrDefault()?.GRNNumber;
                decimal paidFromPO = 0;
                if (!string.IsNullOrEmpty(x.PoNumber))
                {
                    var trimmedPo = x.PoNumber.Trim().ToLower();
                    var matchKey = paymentStatuses.Keys.FirstOrDefault(k => k.Trim().ToLower() == trimmedPo);
                    if (matchKey != null)
                    {
                        paidFromPO = paymentStatuses[matchKey];
                    }
                }

                decimal paidFromGRN = 0;
                if (x.GrnHeaders != null)
                {
                    foreach (var gh in x.GrnHeaders)
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
                decimal totalBilled = x.GrnHeaders != null ? x.GrnHeaders.Where(g => g.Status != "Cancelled").Sum(g => g.TotalAmount) : 0;
                decimal actualPaidAmount = paidFromPO + paidFromGRN;

                decimal baseAmount = (x.Status == "Received" && totalBilled > 0) ? totalBilled : x.GrandTotal;
                decimal due = Math.Max(0, baseAmount - items.Sum(i => i.ReturnAmount) - actualPaidAmount);
                bool isFullyPaid = (supplierBalances.ContainsKey(x.SupplierId) && supplierBalances[x.SupplierId] <= 0.05m) || due <= 0.05m;

                var hasRefund = items.Any(i => {
                    var refunded = _context.PurchaseReturnItems
                        .Where(ri => ri.ProductId == i.ProductId && 
                                     (ri.PurchaseReturn.Status == "Refund" || ri.PurchaseReturn.Status == "Confirmed") &&
                                     _context.GRNHeaders.Any(gh => gh.GRNNumber == ri.GrnRef && gh.PurchaseOrderId == x.Id))
                        .Sum(ri => (decimal?)ri.ReturnQty) ?? 0;
                    return refunded > 0;
                });

                var hasPendingRefund = items.Any(i => {
                    var pendingRefund = _context.PurchaseReturnItems
                        .Where(ri => ri.ProductId == i.ProductId && 
                                     ri.PurchaseReturn.Status == "Confirmed" &&
                                     _context.GRNHeaders.Any(gh => gh.GRNNumber == ri.GrnRef && gh.PurchaseOrderId == x.Id))
                        .Sum(ri => (decimal?)ri.ReturnQty) ?? 0;
                    return pendingRefund > 0;
                });

                var isFulfillmentComplete = items.All(i => {
                    var refunded = _context.PurchaseReturnItems
                        .Where(ri => ri.ProductId == i.ProductId && 
                                     (ri.PurchaseReturn.Status == "Refund" || ri.PurchaseReturn.Status == "Confirmed") &&
                                     _context.GRNHeaders.Any(gh => gh.GRNNumber == ri.GrnRef && gh.PurchaseOrderId == x.Id))
                        .Sum(ri => (decimal?)ri.ReturnQty) ?? 0;
                    return (i.ReceivedQty + refunded) >= i.Qty;
                });

                var poDto = new PurchaseOrderDto
                {
                    Id = x.Id,
                    PoNumber = x.PoNumber,
                    SupplierId = x.SupplierId,
                    SupplierName = x.SupplierName,
                    PoDate = x.PoDate,
                    TotalTax = x.TotalTax,
                    GrandTotal = x.GrandTotal,
                    SubTotal = x.SubTotal,
                    TotalBilled = totalBilled,
                    PaidAmount = actualPaidAmount, // Dynamically mapped from Ledger payments
                    SupplierBalance = supplierBalances.ContainsKey(x.SupplierId) ? supplierBalances[x.SupplierId] : null,
                    ExpectedDeliveryDate = x.ExpectedDeliveryDate,
                    CreatedBy = x.CreatedBy,
                    CreatedOn = x.CreatedOn ?? DateTime.MinValue,
                    ModifiedOn = x.ModifiedOn,
                    Remarks = x.Remarks,
                    HasPendingRefund = hasPendingRefund,
                    IsDispatched = x.IsDispatched,
                    BranchId = x.BranchId,
                    Status = (x.Status == "Cancelled") 
                             ? x.Status 
                             : (x.Status == "Closed" || x.Status == "ShortClosed" || x.Status == "Fully Returned" || (x.GrnHeaders != null && x.GrnHeaders.Any(g => g.Status != "Cancelled")))
                                 ? (items.Sum(i => i.ReturnQty) == items.Sum(i => i.Qty)
                                     ? "Fully Returned"
                                     : (isFulfillmentComplete 
                                         ? (items.Sum(i => i.ReturnQty) > 0 
                                             ? (isFullyPaid ? "ShortClosed" : "Partially Received") 
                                             : "Received") 
                                         : "Partially Received"))
                                 : x.Status,
                    GrnNumber = grnNumber,
                    GrnId = x.GrnHeaders?.FirstOrDefault()?.Id,

                    Items = items,
                    TotalOrdered = items.Sum(i => i.Qty),
                    TotalReceived = items.Sum(i => i.ReceivedQty),
                    TotalAccepted = items.Sum(i => i.AcceptedQty),
                    TotalRejected = items.Sum(i => i.RejectedQty),
                    TotalReturned = items.Sum(i => i.ReturnQty),
                    TotalReturnedAmount = items.Sum(i => i.ReturnAmount)
                };
                if (isOffline)
                {
                    poDto.PaymentStatus = "Offline";
                }
                return poDto;
            }).ToList();

            return new PurchaseOrderPagedResponse(
                dtos,
                result.Total,
                result.TotalAmount,
                result.TodayCount,
                result.MonthCount
            );
        }
    }
}
