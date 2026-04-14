using MediatR;
using Suppliers.Application.DTOs;
using System;

namespace Suppliers.Application.Features.Suppliers.Commands
{
    public record RecordSupplierPaymentCommand(SupplierPaymentDto PaymentData) : IRequest<Guid>;
}
