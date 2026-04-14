using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Subcategories.Commands.Delete
{
    public sealed record BulkDeleteSubCategoriesCommand(
        List<Guid> Ids
    ) : IRequest;
}
