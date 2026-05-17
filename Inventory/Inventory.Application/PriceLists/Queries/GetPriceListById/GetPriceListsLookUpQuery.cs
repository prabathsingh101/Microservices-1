using Inventory.Application.PriceLists.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PriceLists.Queries.GetPriceListById
{
    public record GetPriceListsLookUpQuery(Guid? CompanyId = null, string? PriceType = null) : IRequest<List<PriceListDto>>;
}
