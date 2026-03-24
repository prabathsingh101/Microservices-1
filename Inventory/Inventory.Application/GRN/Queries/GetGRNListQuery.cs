using Inventory.Application.GRN.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.GRN.Queries
{
    public record GetGRNListQuery(string Search, string SortField, string SortOrder, int PageIndex, int PageSize, bool IsQuick = false)
    : IRequest<GRNPagedResponseDto>;
}
