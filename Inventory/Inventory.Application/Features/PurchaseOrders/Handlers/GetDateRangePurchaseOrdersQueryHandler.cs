using AutoMapper;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Features.PurchaseOrders.Queries;
using Inventory.Application.PurchaseOrders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore; 

namespace Inventory.Application.Features.PurchaseOrders.Handlers
{
    public class GetDateRangePurchaseOrdersQueryHandler : IRequestHandler<GetDateRangePurchaseOrdersQuery, PurchaseOrderPagedResponse>
    {
        private readonly IPurchaseOrderRepository _repo;
        private readonly IInventoryDbContext _context; 

        public GetDateRangePurchaseOrdersQueryHandler(
            IPurchaseOrderRepository repo, 
            IInventoryDbContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task<PurchaseOrderPagedResponse> Handle(GetDateRangePurchaseOrdersQuery query, CancellationToken ct)
        {
            // 1. Fetch PO Data with stats
            var result = await _repo.GetDateRangePagedOrdersAsync(query.Request);

            // 2. Mapping with Net Quantity Logic
            var dtos = result.Data.Select(x => {
                var items = x.Items.Select(item => {
                    // Fetch all GRN Details for this specific PO Item
                    var grnSummary = _context.GRNDetails
                        .Where(gd => gd.ProductId == item.ProductId && gd.GRNHeader.PurchaseOrderId == x.Id)
                        .Select(gd => new { gd.ReceivedQty, gd.RejectedQty })
                        .ToList();

                    // Dynamic calculation (Received - Rejected) handles both returns and original rejections accurately
                    var totalAccepted = grnSummary.Sum(s => s.ReceivedQty - s.RejectedQty);
                    var totalRejected = grnSummary.Sum(s => s.RejectedQty);
                    if (totalAccepted < 0) totalAccepted = 0;

                    // Fetch total returned quantity for this specific PO item
                    var totalReturned = _context.PurchaseReturnItems
                        .Where(ri => ri.ProductId == item.ProductId && 
                                     _context.GRNHeaders.Any(gh => gh.GRNNumber == ri.GrnRef && gh.PurchaseOrderId == x.Id))
                        .Sum(ri => (decimal?)ri.ReturnQty) ?? 0;

                    return new PurchaseOrderItemDto
                    {
                        Id = item.Id,
                        Qty = item.Qty, 
                        Unit = item.Unit,
                        Rate = item.Rate,
                        Total = item.Total,
                        TaxAmount = item.TaxAmount,
                        DiscountPercent = item.DiscountPercent,
                        GstPercent = item.GstPercent,
                        ProductName = item.Product != null ? item.Product.Name : "N/A",

                        // Use the field from PurchaseOrderItems table (which is net-updated by repo)
                        ReceivedQty = item.ReceivedQty,
                        AcceptedQty = totalAccepted,
                        RejectedQty = totalRejected,
                        ReturnQty = totalReturned,

                        // Pending = (Ordered - NetReceived)
                        PendingQty = item.Qty - item.ReceivedQty,
                        ManufacturingDate = item.MfgDate,
                        ExpiryDate = item.ExpDate,
                        IsExpiryRequired = item.Product != null ? item.Product.IsExpiryRequired : false,
                        WarehouseName = _context.GRNDetails
                            .Where(gd => gd.ProductId == item.ProductId && gd.GRNHeader.PurchaseOrderId == x.Id)
                            .OrderByDescending(gd => gd.Id)
                            .Select(gd => gd.Warehouse != null ? gd.Warehouse.Name : null)
                            .FirstOrDefault(),
                        RackName = _context.GRNDetails
                            .Where(gd => gd.ProductId == item.ProductId && gd.GRNHeader.PurchaseOrderId == x.Id)
                            .OrderByDescending(gd => gd.Id)
                            .Select(gd => gd.Rack != null ? gd.Rack.Name : null)
                            .FirstOrDefault()
                    };
                }).ToList();

                return new PurchaseOrderDto
                {
                    Id = x.Id,
                    PoNumber = x.PoNumber,
                    SupplierName = x.SupplierName,
                    PoDate = x.PoDate,
                    TotalTax = x.TotalTax,
                    GrandTotal = x.GrandTotal,
                    SubTotal = x.SubTotal,
                    ExpectedDeliveryDate = x.ExpectedDeliveryDate,
                    CreatedBy = x.CreatedBy,
                    CreatedDate = x.CreatedDate ?? DateTime.MinValue,
                    UpdatedDate = x.UpdatedDate,
                    Remarks = x.Remarks,
                    IsDispatched = x.IsDispatched,
                    Status = (x.GrnHeaders != null && x.GrnHeaders.Any())
                             ? (x.Items.All(i => i.ReceivedQty >= i.Qty) ? "Received" : "Partially Received")
                             : x.Status,

                    Items = items,
                    TotalOrdered = items.Sum(i => i.Qty),
                    TotalReceived = items.Sum(i => i.ReceivedQty),
                    TotalAccepted = items.Sum(i => i.AcceptedQty),
                    TotalRejected = items.Sum(i => i.RejectedQty)
                };
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