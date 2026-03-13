using Inventory.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.Stock.Commands.RejectStock
{
    public class RejectStockHandler : IRequestHandler<RejectStockCommand, bool>
    {
        private readonly IInventoryDbContext _context;

        public RejectStockHandler(IInventoryDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(RejectStockCommand request, CancellationToken ct)
        {
            // 1. Find the product
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);
            if (product == null) throw new Exception($"Product with ID {request.ProductId} not found.");

            // 2. Query GRN Details
            var query = _context.GRNDetails
                .Where(g => g.ProductId == request.ProductId &&
                            g.WarehouseId == request.WarehouseId &&
                            g.RackId == request.RackId);

            // Fetch batches to perform matching safely
            var allLocationBatches = await query.ToListAsync(ct);
            
            // Match by Date (Ignoring time part)
            var targetDate = request.ExpiryDate?.Date;
            var batches = allLocationBatches
                .Where(g => (g.ExpDate?.Date == targetDate) || (g.ExpDate == null && targetDate == null))
                .OrderBy(g => g.Id)
                .ToList();

            if (!batches.Any()) 
                throw new Exception($"No matching stock batch found for Product: {product.Name} at the specified location/expiry.");

            decimal remainingToReject = request.Quantity;
            decimal totalBatchAvailable = batches.Sum(b => b.ReceivedQty - b.RejectedQty);

            if (totalBatchAvailable < request.Quantity)
                throw new Exception($"Insufficient quantity in batches. Available: {totalBatchAvailable}, Requested: {request.Quantity}");

            foreach (var batch in batches)
            {
                if (remainingToReject <= 0) break;
                decimal batchAvailable = batch.ReceivedQty - batch.RejectedQty;
                if (batchAvailable <= 0) continue;

                decimal toReject = Math.Min(batchAvailable, remainingToReject);
                batch.RejectedQty += toReject;
                remainingToReject -= toReject;
            }

            decimal actualRejected = request.Quantity - remainingToReject;
            product.CurrentStock -= actualRejected;
            if (product.CurrentStock < 0) product.CurrentStock = 0;

            return await _context.SaveChangesAsync(ct) > 0;
        }
    }
}
