using Inventory.Application.Common.Interfaces;
using Inventory.Application.Common.Models;
using Inventory.Application.GatePasses.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.GatePasses.Queries.GetGatePassesPaged
{
    public class GetGatePassesHandler : IRequestHandler<GetGatePassesQuery, PagedResponse<GatePassDto>>
    {
        private readonly IInventoryDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetGatePassesHandler(IInventoryDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<PagedResponse<GatePassDto>> Handle(GetGatePassesQuery request, CancellationToken cancellationToken)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var query = _context.GatePasses.AsNoTracking().Where(x => x.CompanyId == companyId).AsQueryable();

            // 1. GLOBAL SEARCH
            if (!string.IsNullOrEmpty(request.Filter))
            {
                var term = request.Filter.Trim().ToLower();
                query = query.Where(x => 
                    x.PassNo.ToLower().Contains(term) || 
                    x.PartyName.ToLower().Contains(term) || 
                    x.ReferenceNo.ToLower().Contains(term) ||
                    x.VehicleNo.ToLower().Contains(term) ||
                    x.DriverName.ToLower().Contains(term));
            }

            // 2. DATE RANGE
            if (request.FromDate.HasValue)
            {
                query = query.Where(x => x.GateEntryTime >= request.FromDate.Value);
            }
            if (request.ToDate.HasValue)
            {
                var endDate = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.GateEntryTime <= endDate);
            }

            // 3. TOTAL COUNT (Before Paging)
            var totalCount = await query.CountAsync(cancellationToken);

            // 4. DYNAMIC SORTING
            bool isDesc = request.SortOrder?.ToLower() == "desc";
            string sortField = request.SortField?.ToLower().Trim() ?? "createdon";

            query = sortField switch
            {
                "passno" => isDesc ? query.OrderByDescending(x => x.PassNo) : query.OrderBy(x => x.PassNo),
                "partyname" => isDesc ? query.OrderByDescending(x => x.PartyName) : query.OrderBy(x => x.PartyName),
                "gateentrytime" or "entrytime" => isDesc ? query.OrderByDescending(x => x.GateEntryTime) : query.OrderBy(x => x.GateEntryTime),
                "referenceno" => isDesc ? query.OrderByDescending(x => x.ReferenceNo) : query.OrderBy(x => x.ReferenceNo),
                "vehicleno" => isDesc ? query.OrderByDescending(x => x.VehicleNo) : query.OrderBy(x => x.VehicleNo),
                "createdon" => isDesc ? query.OrderByDescending(x => x.CreatedOn) : query.OrderBy(x => x.CreatedOn),
                _ => query.OrderByDescending(x => x.CreatedOn)
            };

            // 5. PAGINATION
            var items = await query
                .Skip(request.PageIndex * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // 6. MAPPING
            var dtos = items.Select(x => GatePassDto.FromEntity(x)).ToList();

            return new PagedResponse<GatePassDto>(dtos, totalCount, request.PageIndex, request.PageSize);
        }
    }
}
