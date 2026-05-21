using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Finance.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Finance.Handlers
{
    public class ReconcileTransactionHandler : IRequestHandler<ReconcileTransactionCommand, bool>
    {
        private readonly IFinanceRepository _repository;

        public ReconcileTransactionHandler(IFinanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(ReconcileTransactionCommand request, CancellationToken cancellationToken)
        {
            return await _repository.ReconcileTransactionAsync(
                request.RequestData.StatementLineId,
                request.RequestData.MatchedTransactionType,
                request.RequestData.MatchedTransactionId
            );
        }
    }
}
