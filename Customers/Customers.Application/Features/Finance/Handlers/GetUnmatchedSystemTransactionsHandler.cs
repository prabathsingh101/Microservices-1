using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.DTOs;
using Customers.Application.Features.Finance.Queries;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Finance.Handlers
{
    public class GetUnmatchedSystemTransactionsHandler : IRequestHandler<GetUnmatchedSystemTransactionsQuery, List<ReceiptReportDto>>
    {
        private readonly IFinanceRepository _repository;

        public GetUnmatchedSystemTransactionsHandler(IFinanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ReceiptReportDto>> Handle(GetUnmatchedSystemTransactionsQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetUnmatchedSystemTransactionsAsync(request.TransactionType, request.BranchId);
        }
    }
}
