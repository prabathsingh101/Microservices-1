using Inventory.Application.Common.Interfaces;
using Inventory.Application.GRN.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.GRN.Queries
{
    public class GetGRNListHandler : IRequestHandler<GetGRNListQuery, GRNPagedResponseDto>
    {
        private readonly IGRNRepository _repo;
        public GetGRNListHandler(IGRNRepository repo) => _repo = repo;

        public async Task<GRNPagedResponseDto> Handle(GetGRNListQuery request, CancellationToken ct)
        {
            return await _repo.GetGRNPagedListAsync(request.Search, request.SortField, request.SortOrder, request.PageIndex, request.PageSize, request.IsQuick);
        }
    }
}
