using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Products.Commands.DeleteProduct
{
    public sealed record BulkDeleteProductCommand(
         List<Guid> Ids
     ) : IRequest;
}
