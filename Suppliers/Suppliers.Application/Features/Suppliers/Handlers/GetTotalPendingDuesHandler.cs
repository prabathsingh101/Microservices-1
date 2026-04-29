using MediatR;
using Suppliers.Application.Features.Suppliers.Queries;
using Suppliers.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class GetTotalPendingDuesHandler(IFinanceRepository repository) : IRequestHandler<GetTotalPendingDuesQuery, decimal>
    {
        private readonly IFinanceRepository _repository = repository;

        public async Task<decimal> Handle(GetTotalPendingDuesQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetTotalPendingDuesAsync(request.BranchId, request.CompanyId);
        }
    }
}
