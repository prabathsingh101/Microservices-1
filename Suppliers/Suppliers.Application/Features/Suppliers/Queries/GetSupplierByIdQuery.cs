using MediatR;
using Suppliers.Application.DTOs;
using System;

namespace Suppliers.Application.Features.Suppliers.Queries
{
    public record GetSupplierByIdQuery(Guid Id) : IRequest<SupplierDto?>;
}
