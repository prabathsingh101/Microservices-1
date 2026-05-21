using MediatR;
using Customers.Application.DTOs;
using System;

namespace Customers.Application.Features.Finance.Commands
{
    public record RecordPaymentReminderCommand(PaymentReminderLogDto ReminderData) : IRequest<Guid>;
}
