using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Commands;
using Customers.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Customers.Application.Features.Handlers;

public class CreateCustomerHandler
    : IRequestHandler<CreateCustomerCommand, Guid>
{
    private readonly ICustomerRepository _repo;

    public CreateCustomerHandler(ICustomerRepository repo)
    {
        _repo = repo;
    }

    public async Task<Guid> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        // 🚨 DUPLICATE CHECKS within same Company/Tenant
        // 1. Phone number check (Required field)
        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            bool isPhoneDuplicate = await _repo.Query()
                .AnyAsync(c => c.Phone == dto.Phone, cancellationToken);
            if (isPhoneDuplicate)
                throw new InvalidOperationException("A customer with this phone number already exists.");
        }

        // 2. GST Number check (Optional field)
        if (!string.IsNullOrWhiteSpace(dto.GstNumber))
        {
            bool isGstDuplicate = await _repo.Query()
                .AnyAsync(c => c.GstNumber == dto.GstNumber, cancellationToken);
            if (isGstDuplicate)
                throw new InvalidOperationException("A customer with this GST number already exists.");
        }

        // 3. Email check (Optional field)
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            bool isEmailDuplicate = await _repo.Query()
                .AnyAsync(c => c.Email == dto.Email, cancellationToken);
            if (isEmailDuplicate)
                throw new InvalidOperationException("A customer with this email address already exists.");
        }

        var customer = new Customer(
            dto.CustomerName ?? string.Empty,
            dto.CustomerType ?? "Regular",
            dto.Phone ?? string.Empty,
            dto.Email,
            dto.GstNumber,
            dto.CreditLimit, // Changed from dto.CreditLimit ?? 0m because it's not nullable
            new Address(dto.BillingAddress ?? string.Empty),
            string.IsNullOrWhiteSpace(dto.ShippingAddress)
                ? null
                : new Address(dto.ShippingAddress),
            dto.CustomerStatus ?? "Active",
            dto.CreatedBy ?? "System",
            dto.DrugLicenseNo
        );

        await _repo.AddAsync(customer);

        return customer.Id;
    }
}
