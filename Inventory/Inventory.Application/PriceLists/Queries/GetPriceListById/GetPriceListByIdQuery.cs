using Inventory.Application.PriceLists.DTOs;
using MediatR;

namespace Inventory.Application.PriceLists.Queries.GetPriceListById;

public sealed record GetPriceListByIdQuery(Guid Id)
    : IRequest<PriceListDto>;
