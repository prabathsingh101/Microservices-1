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
    public class GetDeliveryChallanListQuery : IRequest<List<DeliveryChallanDto>>
    {
    }

    public class GetDeliveryChallanListHandler : IRequestHandler<GetDeliveryChallanListQuery, List<DeliveryChallanDto>>
    {
        private readonly IInventoryDbContext _context;

        public GetDeliveryChallanListHandler(IInventoryDbContext context)
        {
            _context = context;
        }

        public async Task<List<DeliveryChallanDto>> Handle(GetDeliveryChallanListQuery request, CancellationToken cancellationToken)
        {
            var list = await _context.DeliveryChallans
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
                CancelReason = challan.CancelReason,
                GrossWeight = challan.GrossWeight,
                VehicleRegNo = challan.VehicleRegNo,
                Origin = challan.Origin,
                Destination = challan.Destination,
                CompanyId = challan.CompanyId,
                BranchId = challan.BranchId,
                CreatedBy = challan.CreatedBy
            }).ToList();
        }
    }
}
