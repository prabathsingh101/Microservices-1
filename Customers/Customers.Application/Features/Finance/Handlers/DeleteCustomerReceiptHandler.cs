using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Finance.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Customers.Application.Common;

namespace Customers.Application.Features.Finance.Handlers
{
    public class DeleteCustomerReceiptHandler : IRequestHandler<DeleteCustomerReceiptCommand, bool>
    {
        private readonly IFinanceRepository _repository;
        private readonly ILogger<DeleteCustomerReceiptHandler> _logger;

        public DeleteCustomerReceiptHandler(IFinanceRepository repository, ILogger<DeleteCustomerReceiptHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteCustomerReceiptCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await CustomerLedgerLock.Semaphore.WaitAsync(cancellationToken);
                try
                {
                    var result = await _repository.DeleteReceiptAsync(request.Id);
                    return result;
                }
                finally
                {
                    CustomerLedgerLock.Semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer receipt with ID {Id}", request.Id);
                throw;
            }
        }
    }
}
