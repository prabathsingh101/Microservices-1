using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Finance.Commands;
using Customers.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Finance.Handlers
{
    public class RecordPaymentReminderHandler : IRequestHandler<RecordPaymentReminderCommand, Guid>
    {
        private readonly IFinanceRepository _repository;

        public RecordPaymentReminderHandler(IFinanceRepository repository)
        {
            _repository = repository;
        }

        public async Task<Guid> Handle(RecordPaymentReminderCommand request, CancellationToken cancellationToken)
        {
            var log = new PaymentReminderLog
            {
                CustomerId = request.ReminderData.CustomerId,
                CustomerName = request.ReminderData.CustomerName,
                Phone = request.ReminderData.Phone,
                OutstandingAmount = request.ReminderData.OutstandingAmount,
                ReminderType = request.ReminderData.ReminderType,
                SentStatus = request.ReminderData.SentStatus,
                SentMessage = request.ReminderData.SentMessage
            };

            await _repository.RecordPaymentReminderAsync(log);
            return log.Id;
        }
    }
}
