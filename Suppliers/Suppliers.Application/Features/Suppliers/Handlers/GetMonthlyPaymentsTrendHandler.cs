using MediatR;
using Suppliers.Application.Common.Interfaces;
using Suppliers.Application.Features.Suppliers.Queries;
using Suppliers.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Suppliers.Application.Features.Suppliers.Handlers
{
    public class GetMonthlyPaymentsTrendHandler : IRequestHandler<GetMonthlyPaymentsTrendQuery, List<MonthlyTrendDto>>
    {
        private readonly IFinanceRepository _repository;

        public GetMonthlyPaymentsTrendHandler(IFinanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<MonthlyTrendDto>> Handle(GetMonthlyPaymentsTrendQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetMonthlyTrendAsync(request.Months);
        }
    }
}
