using System.Collections.Generic;
using Inventory.Application.Units.DTOs;
using MediatR;

namespace Inventory.Application.Units.Command
{
    public record CreateBulkUnitsCommand(List<UnitRequestDto> Units) : IRequest<bool>;
    public record UpdateUnitCommand(Guid Id, string Name, string Description, bool IsActive) : IRequest<bool>;
    public record DeleteUnitCommand(Guid Id) : IRequest<bool>;
}
