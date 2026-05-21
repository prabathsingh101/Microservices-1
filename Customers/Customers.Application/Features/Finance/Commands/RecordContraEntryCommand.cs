using MediatR;
using Customers.Application.DTOs;
using System;

namespace Customers.Application.Features.Finance.Commands
{
    public record RecordContraEntryCommand(ContraEntryDto ContraData) : IRequest<Guid>;
}
