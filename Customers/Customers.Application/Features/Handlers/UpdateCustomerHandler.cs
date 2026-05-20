using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Commands;
using Customers.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

            // 🚨 DUPLICATE CHECKS within same Company/Tenant (excluding this customer)
            // 1. Phone number check
            if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                bool isPhoneDuplicate = await _repo.Query()
                    .AnyAsync(c => c.Id != request.Id && c.Phone == dto.Phone, cancellationToken);
                if (isPhoneDuplicate)
                    throw new InvalidOperationException("A customer with this phone number already exists.");
            }

            // 2. GST Number check
            if (!string.IsNullOrWhiteSpace(dto.GstNumber))
            {
                bool isGstDuplicate = await _repo.Query()
                    .AnyAsync(c => c.Id != request.Id && c.GstNumber == dto.GstNumber, cancellationToken);
                if (isGstDuplicate)
                    throw new InvalidOperationException("A customer with this GST number already exists.");
            }

            // 3. Email check
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                bool isEmailDuplicate = await _repo.Query()
                    .AnyAsync(c => c.Id != request.Id && c.Email == dto.Email, cancellationToken);
                if (isEmailDuplicate)
                    throw new InvalidOperationException("A customer with this email address already exists.");
            }

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
