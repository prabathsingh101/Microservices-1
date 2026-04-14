using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Categories.Commands.DeleteCategory
{
    public sealed record BulkDeleteCategoriesCommand(
        List<Guid> Ids
    ) : IRequest;
}
