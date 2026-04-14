using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Commands;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Handlers
{
    public class DeleteCustomerHandler : IRequestHandler<DeleteCustomerCommand, bool>
    {
        private readonly ICustomerRepository _repo;

        public DeleteCustomerHandler(ICustomerRepository repo)
        {
            _repo = repo;
        }

        public async Task<bool> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = await _repo.GetByIdAsync(request.Id);

            if (customer == null) return false;

            await _repo.DeleteAsync(customer);

            return true;
        }
    }
}
