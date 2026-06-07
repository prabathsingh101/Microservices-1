using MediatR;
using System;

namespace Suppliers.Application.Features.Suppliers.Commands
{
    public record DeleteSupplierPaymentCommand(Guid Id) : IRequest<bool>;
}
