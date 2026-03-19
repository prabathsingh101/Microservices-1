using System;
using System.Collections.Generic;
using Inventory.Application.Products.DTOs;
using MediatR;

namespace Inventory.Application.Products.Queries.GetProductTransactions
{
    public record GetProductTransactionsQuery(Guid ProductId) : IRequest<List<ProductTransactionDto>>;
}
