using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Products.Queries.GetProductTransactions
{
    public sealed class GetProductTransactionsHandler
        : IRequestHandler<GetProductTransactionsQuery, List<ProductTransactionDto>>
    {
        private readonly IInventoryDbContext _db;

        public GetProductTransactionsHandler(IInventoryDbContext db)
        {
            _db = db;
        }

        public async Task<List<ProductTransactionDto>> Handle(
            GetProductTransactionsQuery request,
            CancellationToken cancellationToken)
        {
            return await _db.InventoryTransactions
                .AsNoTracking()
                .Where(t => t.ProductId == request.ProductId)
                .OrderByDescending(t => t.CreatedOn)
                .Select(t => new ProductTransactionDto
                {
                    Id = t.Id,
                    CreatedOn = t.CreatedOn,
                    TransactionType = t.TransactionType,
                    ReferenceId = t.ReferenceId,
                    Quantity = t.Quantity,
                    WarehouseName = t.WarehouseId != null ? _db.Warehouses.Where(w => w.Id == t.WarehouseId).Select(w => w.Name).FirstOrDefault() ?? "" : "",
                    RackName = t.RackId != null ? _db.Racks.Where(r => r.Id == t.RackId).Select(r => r.Name).FirstOrDefault() ?? "" : ""
                })
                .ToListAsync(cancellationToken);
        }
    }
}
