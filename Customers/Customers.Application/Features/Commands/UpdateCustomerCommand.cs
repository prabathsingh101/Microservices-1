using Customers.Application.DTOs;
using MediatR;
using System;

namespace Customers.Application.Features.Commands
{
    public class UpdateCustomerCommand : IRequest<bool>
    {
        public Guid Id { get; }
        public CreateCustomerDto Dto { get; }

        public UpdateCustomerCommand(Guid id, CreateCustomerDto dto)
        {
            Id = id;
            Dto = dto;
        }
    }
}
