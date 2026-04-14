using Customers.Application.Common.Interfaces;
using Customers.Application.Features.Commands;
using Customers.Domain.Entities;
using MediatR;
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
            dto.CreatedBy ?? "System"
        );

        await _repo.AddAsync(customer);

        return customer.Id;
    }
}
