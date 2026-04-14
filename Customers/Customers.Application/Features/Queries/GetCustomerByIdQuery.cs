using MediatR;
using Customers.Application.Common.Interfaces;
using Customers.Application.DTOs;
using System;

namespace Customers.Application.Features.Queries
{
    public class GetCustomerByIdQuery : IRequest<CustomerDto>
    {
        public Guid Id { get; set; }

        public GetCustomerByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
