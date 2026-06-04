using Inventory.Application.Common.Interfaces;
using Inventory.Application.GatePasses.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.GatePasses.Queries.GetVehicleAutocomplete
{
    public class GetVehicleAutocompleteHandler : IRequestHandler<GetVehicleAutocompleteQuery, List<VehicleAutocompleteDto>>
    {
        private readonly IInventoryDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetVehicleAutocompleteHandler(IInventoryDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<VehicleAutocompleteDto>> Handle(GetVehicleAutocompleteQuery request, CancellationToken cancellationToken)
        {
            var searchTerm = request.SearchTerm?.Trim().ToLower() ?? "";
            var companyId = _currentUserService.CompanyId;

            // Get unique vehicles with their latest driver details across all branches company-wide
            var query = _context.GatePasses
                .IgnoreQueryFilters()
                .AsNoTracking();

            if (companyId.HasValue)
            {
                query = query.Where(x => x.CompanyId == companyId.Value);
            }

            var vehicles = await query
                .Where(x => x.VehicleNo.ToLower().Contains(searchTerm))
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new 
                {
                    x.VehicleNo,
                    x.DriverName,
                    x.DriverPhone,
                    x.VehicleType
                })
                .ToListAsync(cancellationToken);

            // Group by VehicleNo and pick the first one (latest due to OrderByDescending CreatedAt)
            var result = vehicles
                .GroupBy(x => x.VehicleNo.ToUpper())
                .Select(g => g.First())
                .Select(x => new VehicleAutocompleteDto
                {
                    VehicleNo = x.VehicleNo,
                    DriverName = x.DriverName,
                    DriverPhone = x.DriverPhone,
                    VehicleType = x.VehicleType
                })
                .Take(10) // Limit results
                .ToList();

            return result;
        }
    }
}
