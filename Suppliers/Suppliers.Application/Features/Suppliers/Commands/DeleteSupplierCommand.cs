using MediatR;
using System;

namespace Suppliers.Application.Features.Suppliers.Commands
{
    public record DeleteSupplierCommand(Guid Id) : IRequest<bool>;
}
