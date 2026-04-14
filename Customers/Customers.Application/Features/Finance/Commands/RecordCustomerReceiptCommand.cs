using MediatR;
using Customers.Application.DTOs;

namespace Customers.Application.Features.Finance.Commands
{
    public record RecordCustomerReceiptCommand(CustomerReceiptDto ReceiptData) : IRequest<Guid>;
}
