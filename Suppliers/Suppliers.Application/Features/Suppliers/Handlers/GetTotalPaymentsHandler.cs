using MediatR;
using Suppliers.Application.Features.Suppliers.Queries;
using Suppliers.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class GetTotalPaymentsHandler(IFinanceRepository repository) : IRequestHandler<GetTotalPaymentsQuery, decimal>
    {
        private readonly IFinanceRepository _repository = repository;

        public async Task<decimal> Handle(GetTotalPaymentsQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetTotalPaymentsAsync(request.DateRange);
        }
    }
}
