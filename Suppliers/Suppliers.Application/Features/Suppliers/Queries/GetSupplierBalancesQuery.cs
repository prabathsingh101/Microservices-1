using MediatR;
using System;
using System.Collections.Generic;

namespace Suppliers.Application.Features.Suppliers.Queries
{
    public record GetSupplierBalancesQuery(List<Guid> SupplierIds) : IRequest<Dictionary<Guid, decimal>>;
}
