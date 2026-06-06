using MediatR;
using System;

namespace Customers.Application.Features.Finance.Commands
{
    public record DeleteCustomerReceiptCommand(Guid Id) : IRequest<bool>;
}
