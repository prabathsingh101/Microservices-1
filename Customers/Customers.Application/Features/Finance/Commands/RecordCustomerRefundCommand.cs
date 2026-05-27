using MediatR;
using Customers.Application.DTOs;

namespace Customers.Application.Features.Finance.Commands
{
    public record RecordCustomerRefundCommand(CustomerRefundDto RefundData) : IRequest<Guid>;
}
