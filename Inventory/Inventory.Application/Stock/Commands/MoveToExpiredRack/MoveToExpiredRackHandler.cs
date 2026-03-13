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

            // 2. Find Source Batch
            var targetDate = request.ExpiryDate?.Date;
            var sourceBatches = await _context.GRNDetails
                .Where(g => g.ProductId == request.ProductId &&
                            g.WarehouseId == request.SourceWarehouseId &&
                            g.RackId == request.SourceRackId)
                .ToListAsync(ct);

            var batch = sourceBatches.FirstOrDefault(g => 
                (g.ExpDate?.Date == targetDate) || (g.ExpDate == null && targetDate == null));

            if (batch == null || (batch.ReceivedQty - batch.RejectedQty) < request.Quantity)
                throw new Exception("Source stock batch not found or insufficient quantity.");

            // 3. Move Logic
            batch.ReceivedQty -= request.Quantity; 

            // Find or Create in Expired Rack
            var existingExpiredBatch = await _context.GRNDetails
                .FirstOrDefaultAsync(g => g.ProductId == request.ProductId &&
                                        g.WarehouseId == targetRack.WarehouseId &&
                                        g.RackId == targetRack.Id &&
                                        g.ExpDate == batch.ExpDate, ct);

            if (existingExpiredBatch != null)
            {
                existingExpiredBatch.ReceivedQty += request.Quantity;
            }
            else
            {
                var newBatch = new GRNDetail
                {
                    GRNHeaderId = batch.GRNHeaderId,
                    ProductId = batch.ProductId,
                    ReceivedQty = request.Quantity,
                    UnitRate = batch.UnitRate,
                    GstPercent = batch.GstPercent,
                    MfgDate = batch.MfgDate,
                    ExpDate = batch.ExpDate,
                    RackId = targetRack.Id,
                    WarehouseId = targetRack.WarehouseId,
                    OrderedQty = 0,
                    AcceptedQty = request.Quantity,
                    RejectedQty = 0,
                    PendingQty = 0
                };
                _context.GRNDetails.Add(newBatch);
            }
            
            return await _context.SaveChangesAsync(ct) > 0;
        }
    }
}
