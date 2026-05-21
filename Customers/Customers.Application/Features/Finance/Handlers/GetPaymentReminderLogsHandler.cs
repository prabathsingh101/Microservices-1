using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.DTOs;
using Customers.Application.Features.Finance.Queries;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Finance.Handlers
{
    public class GetPaymentReminderLogsHandler : IRequestHandler<GetPaymentReminderLogsQuery, List<PaymentReminderLogDto>>
    {
        private readonly IFinanceRepository _repository;

        public GetPaymentReminderLogsHandler(IFinanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<PaymentReminderLogDto>> Handle(GetPaymentReminderLogsQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetPaymentReminderLogsAsync(request.CustomerId, request.BranchId);
        }
    }
}
