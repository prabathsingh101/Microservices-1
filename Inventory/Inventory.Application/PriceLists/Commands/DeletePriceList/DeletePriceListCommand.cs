using MediatR;

namespace Inventory.Application.PriceLists.Commands.DeletePriceList;

public sealed record DeletePriceListCommand(Guid Id)
    : IRequest<Guid>;
