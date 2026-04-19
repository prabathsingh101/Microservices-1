using Inventory.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.GatePasses.Queries.CheckDuplicateGatePass
{
    public class CheckDuplicateGatePassHandler : IRequestHandler<CheckDuplicateGatePassQuery, DuplicateGatePassResponse>
    {
        private readonly IInventoryDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CheckDuplicateGatePassHandler(IInventoryDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<DuplicateGatePassResponse> Handle(CheckDuplicateGatePassQuery request, CancellationToken cancellationToken)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            // We check for active gate passes (Entered status = 1) for the same reference number and type
            // If PassType is Inward, it means a gate pass exists but GRN is not yet done (Status would be Completed=4 once GRN is done)
            var existingPass = await _context.GatePasses
                .AsNoTracking()
                .Where(x => x.ReferenceNo == request.ReferenceNo && x.PassType == request.PassType && x.Status == 1 && x.CompanyId == companyId) // 1 = Entered
                .OrderByDescending(x => x.CreatedOn)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingPass != null)
            {
                return new DuplicateGatePassResponse(true, existingPass.PassNo, existingPass.Status);
            }

            return new DuplicateGatePassResponse(false, null, null);
        }
    }
}
