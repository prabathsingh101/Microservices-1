using Inventory.Application.GatePasses.DTOs;
using MediatR;
using System.Collections.Generic;

namespace Inventory.Application.GatePasses.Queries.GetVehicleAutocomplete
{
    public class GetVehicleAutocompleteQuery : IRequest<List<VehicleAutocompleteDto>>
    {
        public string SearchTerm { get; set; }
    }
}
