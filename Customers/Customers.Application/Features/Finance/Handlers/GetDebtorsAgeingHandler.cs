using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.DTOs;
using Customers.Application.Features.Finance.Queries;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Finance.Handlers
{
    public class GetDebtorsAgeingHandler : IRequestHandler<GetDebtorsAgeingQuery, List<DebtorsAgeingDto>>
    {
        private readonly IFinanceRepository _repository;

        public GetDebtorsAgeingHandler(IFinanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<DebtorsAgeingDto>> Handle(GetDebtorsAgeingQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetDebtorsAgeingAsync(request.BranchId);
        }
    }
}
