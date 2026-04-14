using Inventory.Application.Common.Interfaces;
using Inventory.Application.GRN.DTOs;
using MediatR;

namespace Inventory.Application.GRN.Queries
{
    public class GetPOForGRNHandler : IRequestHandler<GetPOForGRNQuery, POForGRNDTO?>
    {
        private readonly IGRNRepository _repo;
        public GetPOForGRNHandler(IGRNRepository repo) => _repo = repo;

        public async Task<POForGRNDTO?> Handle(GetPOForGRNQuery request, CancellationToken ct)
        {
            // Logic Fix: Ab teen parameters pass karein (Replacement logic ke liye gatePassNo)
            return await _repo.GetPODataForGRN(request.poIds, request.GrnHeaderId, request.GatePassNo);
        }
    }
}
