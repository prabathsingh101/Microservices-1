using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.Stock.Commands.MoveToExpiredRack
{
    public class MoveToExpiredRackHandler : IRequestHandler<MoveToExpiredRackCommand, bool>
    {
        private readonly IInventoryDbContext _context;

        public MoveToExpiredRackHandler(IInventoryDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(MoveToExpiredRackCommand request, CancellationToken ct)
        {
            // 1. Find the Expired Rack (Rack E1 / Expired Products)
            var targetRack = await _context.Racks
                .FirstOrDefaultAsync(r => r.Description.Contains("Expired") || r.Name.Contains("E1"), ct);

            if (targetRack == null)
                throw new Exception("Destination 'Expired Rack' not found in system. Please create a rack with description 'Expired Products'.");

            // 2. Find matching batches at the source location
            var targetDate = request.ExpiryDate?.Date;
            var allBatches = await _context.GRNDetails
                .Where(g => g.ProductId == request.ProductId &&
                            g.WarehouseId == request.SourceWarehouseId &&
                            g.RackId == request.SourceRackId)
                .ToListAsync(ct);

            // Filter by Date and exclude empty batches, then sort to pick most viable first
            var matchingBatches = allBatches
                .Where(g => (g.ExpDate?.Date == targetDate) || (g.ExpDate == null && targetDate == null))
                .OrderByDescending(g => (g.ReceivedQty - g.RejectedQty)) 
                .ToList();

            if (!matchingBatches.Any())
                throw new Exception("No matching stock batch found at the specified location/expiry.");

            decimal totalAvailable = matchingBatches.Sum(g => g.ReceivedQty - g.RejectedQty);
            if (totalAvailable < request.Quantity)
                throw new Exception($"Insufficient quantity to move. Available: {totalAvailable}, Requested: {request.Quantity}");

            // 3. Move Logic (Loop through batches)
            decimal remainingToMove = request.Quantity;

            foreach (var b in matchingBatches)
            {
                if (remainingToMove <= 0) break;
                
                decimal batchAvailable = b.ReceivedQty - b.RejectedQty;
                if (batchAvailable <= 0) continue;

                decimal toMove = Math.Min(batchAvailable, remainingToMove);
                
                // Reduce from source
                b.ReceivedQty -= toMove; 
                // We should also keep AcceptedQty in sync if it was used for stock tracking
                if (b.AcceptedQty >= toMove) b.AcceptedQty -= toMove;
                
                remainingToMove -= toMove;

                // 4. Update or Create in Expired Rack
                // Aggregating by ExpDate and UnitRate for valuation consistency
                var existingExpiredBatch = await _context.GRNDetails
                    .FirstOrDefaultAsync(g => g.ProductId == request.ProductId &&
                                            g.WarehouseId == targetRack.WarehouseId &&
                                            g.RackId == targetRack.Id &&
                                            g.ExpDate == b.ExpDate &&
                                            g.UnitRate == b.UnitRate, ct);

                if (existingExpiredBatch != null)
                {
                    existingExpiredBatch.ReceivedQty += toMove;
                    existingExpiredBatch.AcceptedQty += toMove;
                }
                else
                {
                    var newBatch = new GRNDetail
                    {
                        GRNHeaderId = b.GRNHeaderId,
                        ProductId = b.ProductId,
                        ReceivedQty = toMove,
                        UnitRate = b.UnitRate,
                        GstPercent = b.GstPercent,
                        MfgDate = b.MfgDate,
                        ExpDate = b.ExpDate,
                        RackId = targetRack.Id,
                        WarehouseId = targetRack.WarehouseId,
                        OrderedQty = 0,
                        AcceptedQty = toMove,
                        RejectedQty = 0,
                        PendingQty = 0
                    };
                    _context.GRNDetails.Add(newBatch);
                }
            }
            
            return await _context.SaveChangesAsync(ct) > 0;
        }
    }
}
