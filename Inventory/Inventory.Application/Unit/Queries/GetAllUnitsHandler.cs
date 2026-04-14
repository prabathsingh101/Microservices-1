using Inventory.Application.Common.Interfaces;
using Inventory.Application.Units.DTOs;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.Units.Queries
{
    public class GetAllUnitsHandler : IRequestHandler<GetAllUnitsQuery, IEnumerable<Inventory.Application.Units.DTOs.UnitDto>>
    {
        private readonly IUnitRepository _repo;
        public GetAllUnitsHandler(IUnitRepository repo) => _repo = repo;

        public async Task<IEnumerable<Inventory.Application.Units.DTOs.UnitDto>> Handle(GetAllUnitsQuery request, CancellationToken ct)
        {
            var units = await _repo.GetAllAsync();
            return units.Select(u => new Inventory.Application.Units.DTOs.UnitDto(u.Id, u.Name, u.Description, u.IsActive));
        }
    }
}
