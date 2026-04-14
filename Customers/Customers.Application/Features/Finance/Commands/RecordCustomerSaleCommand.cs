using MediatR;
using Customers.Application.DTOs;
using System;

namespace Customers.Application.Features.Finance.Commands
{
    public record RecordCustomerSaleCommand(CustomerSaleDto SaleDto) : IRequest<Guid>;
}
