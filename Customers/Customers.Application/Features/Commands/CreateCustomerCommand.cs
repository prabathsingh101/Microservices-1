using Customers.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Customers.Application.Features.Commands
{
    public record CreateCustomerCommand(CreateCustomerDto Dto)
    : IRequest<Guid>;
}
