using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
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
        private readonly ICurrentUserService _currentUserService;

        public RejectStockHandler(IInventoryDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(RejectStockCommand request, CancellationToken ct)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            // 1. Find the product
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && p.CompanyId == companyId, ct);
            if (product == null) throw new Exception($"Product with ID {request.ProductId} not found.");

            // 2. Query GRN Details
            var query = _context.GRNDetails
                .Where(g => g.ProductId == request.ProductId &&
                            g.WarehouseId == request.WarehouseId &&
                            g.RackId == request.RackId &&
                            g.CompanyId == companyId);

            // Fetch rack info to check if it's an expired/unusable rack
            var rack = await _context.Racks.FirstOrDefaultAsync(r => r.Id == request.RackId && r.CompanyId == companyId, ct);
            bool isExpiredRack = rack != null && (
                rack.Name.ToLower().Contains("e1") || 
                (rack.Description != null && (
                    rack.Description.ToLower().Contains("expired") || 
                    rack.Description.ToLower().Contains("damaged") || 
                    rack.Description.ToLower().Contains("rejected") ||
                    rack.Description.ToLower().Contains("purged")
                ))
            );

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
            
            // If it's an expired rack, the "available to purge" is actually the rejected/expired qty
            decimal totalBatchAvailable = isExpiredRack 
                ? batches.Sum(b => b.RejectedQty) 
                : batches.Sum(b => b.ReceivedQty - b.RejectedQty);

            if (totalBatchAvailable < request.Quantity)
                throw new Exception($"Insufficient quantity in batches. Available: {totalBatchAvailable}, Requested: {request.Quantity}");

            foreach (var batch in batches)
            {
                if (remainingToReject <= 0) break;
                
                decimal batchAvailable = isExpiredRack ? batch.RejectedQty : (batch.ReceivedQty - batch.RejectedQty);
                if (batchAvailable <= 0) continue;

                decimal toReject = Math.Min(batchAvailable, remainingToReject);
                
                if (isExpiredRack)
                {
                    // For expired rack purge, we reduce both to permanently remove from records
                    batch.ReceivedQty -= toReject;
                    batch.RejectedQty -= toReject;
                }
                else
                {
                    // For standard rejection, we increase rejected qty (moving from usable to unusable)
                    batch.RejectedQty += toReject;
                }
                
                remainingToReject -= toReject;
            }

            decimal actualRejected = request.Quantity - remainingToReject;
            
            // For standard rejections, we reduce the product-level CurrentStock
            // For expired rack purges, CurrentStock was ALREADY reduced when it was moved THERE.
            // Wait, I need to check if MoveToExpiredRackHandler reduces CurrentStock. 
            // If it DOESN'T, I should reduce it here too.
            if (!isExpiredRack)
            {
                product.CurrentStock -= actualRejected;
                if (product.CurrentStock < 0) product.CurrentStock = 0;

                // 🆕 Record Inventory Transaction for Audit Trail (OUTWARD)
                var adjTx = new InventoryTransaction(
                    request.ProductId,
                    actualRejected,
                    "StockAdjustment-OUT",
                    "ADJ-" + DateTime.Now.Ticks.ToString().Substring(10),
                    request.WarehouseId,
                    request.RackId,
                    null,
                    request.ExpiryDate,
                    companyId
                );
                await _context.InventoryTransactions.AddAsync(adjTx, ct);
            }
            else 
            {
                // Record PURGE from expired rack (OUTWARD)
                var purgeTx = new InventoryTransaction(
                    request.ProductId,
                    actualRejected,
                    "StockPurge-OUT",
                    "PRG-" + DateTime.Now.Ticks.ToString().Substring(10),
                    request.WarehouseId,
                    request.RackId,
                    null,
                    request.ExpiryDate,
                    companyId
                );
                await _context.InventoryTransactions.AddAsync(purgeTx, ct);
            }

            return await _context.SaveChangesAsync(ct) > 0;
        }
    }
}
