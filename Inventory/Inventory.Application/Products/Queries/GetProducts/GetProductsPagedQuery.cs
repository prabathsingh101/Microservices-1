using Inventory.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Products.Queries.GetProducts
{
    public sealed record GetProductsPagedQuery(GridRequest Request)
     : IRequest<GridResponse<ProductDto>>;
}
