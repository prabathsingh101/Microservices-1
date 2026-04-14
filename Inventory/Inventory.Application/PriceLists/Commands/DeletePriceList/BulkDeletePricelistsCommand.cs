using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.PriceLists.Commands.DeletePriceList
{
    public sealed record BulkDeletePricelistsCommand(
        List<Guid> Ids
    ) : IRequest;
}
