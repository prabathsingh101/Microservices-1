using MediatR;
using Suppliers.Application.DTOs;
using System.Collections.Generic;

namespace Suppliers.Application.Features.Suppliers.Queries
{
    public record GetAllSuppliersQuery() : IRequest<IEnumerable<SupplierDto>>;
}
