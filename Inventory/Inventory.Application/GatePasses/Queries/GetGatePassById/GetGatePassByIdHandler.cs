using Inventory.Application.Common.Interfaces;
using Inventory.Application.GatePasses.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.GatePasses.Queries.GetGatePassById
{
    public class GetGatePassByIdHandler : IRequestHandler<GetGatePassByIdQuery, GatePassDto?>
    {
        private readonly IInventoryDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetGatePassByIdHandler(IInventoryDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<GatePassDto?> Handle(GetGatePassByIdQuery request, CancellationToken cancellationToken)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var entity = await _context.GatePasses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.Id && x.CompanyId == companyId, cancellationToken);
            
            return entity != null ? GatePassDto.FromEntity(entity) : null;
        }
    }
}
