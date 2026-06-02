using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.DeliveryChallans.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.DeliveryChallans.Queries
{
    public class GetPendingDeliveryChallansQuery : IRequest<List<DeliveryChallanDto>>
    {
        public Guid CustomerId { get; set; }
        public GetPendingDeliveryChallansQuery(Guid customerId)
        {
            CustomerId = customerId;
        }
    }

    public class GetPendingDeliveryChallansHandler : IRequestHandler<GetPendingDeliveryChallansQuery, List<DeliveryChallanDto>>
    {
        private readonly IInventoryDbContext _context;

        public GetPendingDeliveryChallansHandler(IInventoryDbContext context)
        {
            _context = context;
        }

        public async Task<List<DeliveryChallanDto>> Handle(GetPendingDeliveryChallansQuery request, CancellationToken cancellationToken)
        {
            var list = await _context.DeliveryChallans
                .Include(x => x.Items)
                .Where(x => x.CustomerId == request.CustomerId && (x.Status == "Pending" || x.Status == "Draft"))
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync(cancellationToken);

            return list.Select(challan => new DeliveryChallanDto
            {
                Id = challan.Id,
                ChallanNo = challan.ChallanNo,
                ChallanDate = challan.ChallanDate,
                CustomerId = challan.CustomerId,
                CustomerName = challan.CustomerName,
                SubTotal = challan.SubTotal,
                TotalTax = challan.TotalTax,
                GrandTotal = challan.GrandTotal,
                Remarks = challan.Remarks,
                Status = challan.Status,
                GrossWeight = challan.GrossWeight,
                VehicleRegNo = challan.VehicleRegNo,
                Origin = challan.Origin,
                Destination = challan.Destination,
                CompanyId = challan.CompanyId,
                BranchId = challan.BranchId,
                CreatedBy = challan.CreatedBy,
                Items = challan.Items.Select(i => new DeliveryChallanItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Qty = i.Qty,
                    Unit = i.Unit,
                    Rate = i.Rate,
                    MRP = i.MRP,
                    DiscountPercent = i.DiscountPercent,
                    DiscountAmount = i.DiscountAmount,
                    GstPercent = i.GSTPercent,
                    TaxAmount = i.TaxAmount,
                    Total = i.Total,
                    WarehouseId = i.WarehouseId,
                    WarehouseName = i.WarehouseId.HasValue ? _context.Warehouses.IgnoreQueryFilters().Where(w => w.Id == i.WarehouseId.Value).Select(w => w.Name).FirstOrDefault() : null,
                    RackId = i.RackId,
                    RackName = i.RackId.HasValue ? _context.Racks.IgnoreQueryFilters().Where(r => r.Id == i.RackId.Value).Select(r => r.Name).FirstOrDefault() : null,
                    BatchNumber = i.BatchNumber,
                    MfgDate = i.MfgDate,
                    ExpDate = i.ExpDate
                }).ToList()
            }).ToList();
        }
    }
}
