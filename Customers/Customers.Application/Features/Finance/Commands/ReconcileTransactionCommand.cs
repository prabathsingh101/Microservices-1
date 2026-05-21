using MediatR;
using Customers.Application.DTOs;

namespace Customers.Application.Features.Finance.Commands
{
    public record ReconcileTransactionCommand(ReconcileTransactionRequestDto RequestData) : IRequest<bool>;
}
