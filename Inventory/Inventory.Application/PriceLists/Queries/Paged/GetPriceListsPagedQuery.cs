using Inventory.Application.Common.Models;
using Inventory.Application.PriceLists.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PriceLists.Queries.Paged
{
    public sealed record GetPriceListsPagedQuery(GridRequest Request)
    : IRequest<GridResponse<PriceListDto>>;
}
