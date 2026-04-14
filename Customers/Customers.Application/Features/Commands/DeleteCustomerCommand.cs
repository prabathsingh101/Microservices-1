using MediatR;
using System;

namespace Customers.Application.Features.Commands
{
    public class DeleteCustomerCommand : IRequest<bool>
    {
        public Guid Id { get; }

        public DeleteCustomerCommand(Guid id)
        {
            Id = id;
        }
    }
}
