using MediatR;
using System.Collections.Generic;
using Inventory.Application.Units.DTOs;

namespace Inventory.Application.Units.Queries
{
    public record GetAllUnitsQuery : IRequest<IEnumerable<Inventory.Application.Units.DTOs.UnitDto>>;
}
