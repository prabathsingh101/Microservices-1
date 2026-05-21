using MediatR;
using Suppliers.Application.Features.Suppliers.Queries;
using Suppliers.Application.Common.Interfaces;
using Suppliers.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class GetTotalAdjustmentsHandler(IFinanceRepository repository) : IRequestHandler<GetTotalAdjustmentsQuery, AdjustmentsSummaryDto>
    {
        private readonly IFinanceRepository _repository = repository;

        public async Task<AdjustmentsSummaryDto> Handle(GetTotalAdjustmentsQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetTotalAdjustmentsAsync(request.DateRange);
        }
    }
}
