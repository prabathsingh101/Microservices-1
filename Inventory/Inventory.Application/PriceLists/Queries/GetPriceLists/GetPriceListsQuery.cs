using MediatR;
using Inventory.Application.PriceLists.DTOs;

namespace Inventory.Application.PriceLists.Queries.GetPriceLists;

public sealed record GetPriceListsQuery
    : IRequest<List<PriceListDto>>;
