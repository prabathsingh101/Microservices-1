using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Finance.Queries;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Finance.Handlers
{
    public class GetTotalOutstandingHandler : IRequestHandler<GetTotalOutstandingQuery, decimal>
    {
        private readonly IFinanceRepository _repository;

        public GetTotalOutstandingHandler(IFinanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<decimal> Handle(GetTotalOutstandingQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetTotalOutstandingAsync(request.BranchId, request.CompanyId);
        }
    }
}
