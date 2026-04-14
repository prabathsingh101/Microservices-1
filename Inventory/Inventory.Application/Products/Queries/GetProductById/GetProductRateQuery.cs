using Inventory.Application.Products.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Products.Queries.GetProductById
{
    // Products.Application/Queries/GetProductRateQuery.cs
    public record GetProductRateQuery(Guid ProductId, Guid PriceListId) : IRequest<ProductRateDto>;
}
