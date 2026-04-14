using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Commands;
using Customers.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Handlers
{
    public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, bool>
    {
        private readonly ICustomerRepository _repo;

        public UpdateCustomerHandler(ICustomerRepository repo)
        {
            _repo = repo;
        }

        public async Task<bool> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = await _repo.GetByIdAsync(request.Id);

            if (customer == null) return false;

            var dto = request.Dto;

            customer.Update(
                dto.CustomerName ?? string.Empty,
                dto.CustomerType ?? "Regular",
                dto.Phone ?? string.Empty,
                dto.Email,
                dto.GstNumber,
                dto.CreditLimit, // Changed from dto.CreditLimit ?? 0m because it's not nullable
                new Address(dto.BillingAddress ?? string.Empty),
                string.IsNullOrWhiteSpace(dto.ShippingAddress) ? null : new Address(dto.ShippingAddress),
                dto.CustomerStatus ?? "Active"
            );

            await _repo.UpdateAsync(customer);

            return true;
        }
    }
}
