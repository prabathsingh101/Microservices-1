using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Finance.Queries;
using Customers.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Finance.Handlers
{
    public class GetMonthlyReceiptsTrendHandler : IRequestHandler<GetMonthlyReceiptsTrendQuery, List<MonthlyTrendDto>>
    {
        private readonly IFinanceRepository _repository;

        public GetMonthlyReceiptsTrendHandler(IFinanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<MonthlyTrendDto>> Handle(GetMonthlyReceiptsTrendQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetMonthlyTrendAsync(request.Months, request.BranchId);
        }
    }
}
