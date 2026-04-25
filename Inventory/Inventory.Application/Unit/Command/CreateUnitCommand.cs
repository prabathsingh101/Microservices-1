using System.Collections.Generic;
using Inventory.Application.Units.DTOs;
using MediatR;

namespace Inventory.Application.Units.Command
{
    public record CreateBulkUnitsCommand(List<UnitRequestDto> Units, Guid CompanyId, string? BranchId = null) : IRequest<bool>;
    public record UpdateUnitCommand(Guid Id, string Name, string Description, bool IsActive, Guid CompanyId, string? BranchId = null) : IRequest<bool>;
    public record DeleteUnitCommand(Guid Id) : IRequest<bool>;
}
