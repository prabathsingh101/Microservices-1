using Customers.Application.DTOs;
using MediatR;
using System.Collections.Generic;

namespace Customers.Application.Features.Queries
{
    public record GetCustomersQuery() : IRequest<List<CustomerDto>>;
}
