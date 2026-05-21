using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Finance.Queries;
using Customers.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Finance.Handlers
{
    public class GetTotalAdjustmentsHandler : IRequestHandler<GetTotalAdjustmentsQuery, AdjustmentsSummaryDto>
    {
        private readonly IFinanceRepository _repository;

        public GetTotalAdjustmentsHandler(IFinanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<AdjustmentsSummaryDto> Handle(GetTotalAdjustmentsQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetTotalAdjustmentsAsync(request.DateRange);
        }
    }
}
