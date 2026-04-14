using Inventory.Application.GatePasses.DTOs;
using MediatR;

namespace Inventory.Application.GatePasses.Queries.GetGatePassById
{
    public record GetGatePassByIdQuery(Guid Id) : IRequest<GatePassDto?>;
}
