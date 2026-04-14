using MediatR;
using Suppliers.Application.DTOs;
using System;

namespace Suppliers.Application.Features.Suppliers.Commands
{
    public record CreateSupplierCommand(CreateSupplierDto SupplierData) : IRequest<Guid>;
}
