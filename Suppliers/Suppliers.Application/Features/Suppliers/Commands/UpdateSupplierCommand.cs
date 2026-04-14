using MediatR;
using Suppliers.Application.DTOs;
using System;

namespace Suppliers.Application.Features.Suppliers.Commands
{
    public record UpdateSupplierCommand(Guid Id, CreateSupplierDto SupplierData) : IRequest<bool>;
}
