using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.DTOs;
using Customers.Application.Features.Finance.Queries;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Finance.Handlers
{
    public class GetBankStatementsHandler : IRequestHandler<GetBankStatementsQuery, List<BankStatementDto>>
    {
        private readonly IFinanceRepository _repository;

        public GetBankStatementsHandler(IFinanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<BankStatementDto>> Handle(GetBankStatementsQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetBankStatementsAsync(request.BranchId);
        }
    }
}
