using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.DTOs;
using Customers.Application.Features.Finance.Queries;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Finance.Handlers
{
    public class GetBankStatementLinesHandler : IRequestHandler<GetBankStatementLinesQuery, List<BankStatementLineDto>>
    {
        private readonly IFinanceRepository _repository;

        public GetBankStatementLinesHandler(IFinanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<BankStatementLineDto>> Handle(GetBankStatementLinesQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetBankStatementLinesAsync(request.StatementId);
        }
    }
}
