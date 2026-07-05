using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.Stock.Commands.MoveStock
{
    public class MoveStockHandler : IRequestHandler<MoveStockCommand, bool>
    {
        private readonly IInventoryDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public MoveStockHandler(IInventoryDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(MoveStockCommand request, CancellationToken ct)
        {
            var companyId = _currentUserService.CompanyId ?? Guid.Empty;
            var branchId = _currentUserService.BranchId;

            // 1. Find the Source Rack and Target Rack
            var sourceRack = await _context.Racks.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == request.SourceRackId && r.CompanyId == companyId, ct);
            if (sourceRack == null)
                throw new Exception("Source rack not found.");

            var targetRack = await _context.Racks.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == request.TargetRackId && r.CompanyId == companyId, ct);
            if (targetRack == null)
                throw new Exception("Target rack not found.");

            bool isSourceExpired = IsExpiredRack(sourceRack.Name, sourceRack.Description);
            bool isTargetExpired = IsExpiredRack(targetRack.Name, targetRack.Description);

            // 2. Find matching batches at the source location
            var targetDate = request.ExpiryDate?.Date;
            var allBatches = await _context.GRNDetails
                .Where(g => g.ProductId == request.ProductId &&
                            g.WarehouseId == request.SourceWarehouseId &&
                            g.RackId == request.SourceRackId &&
                            g.CompanyId == companyId)
                .ToListAsync(ct);

            var matchingBatches = allBatches
                .Where(g => (g.ExpDate?.Date == targetDate) || (g.ExpDate == null && targetDate == null))
                .OrderByDescending(g => Math.Max(g.RejectedQty, g.ReceivedQty - g.RejectedQty))
                .ToList();

            if (!matchingBatches.Any())
                throw new Exception("No matching stock batch found at the specified location/expiry.");

            decimal totalAvailable = matchingBatches.Sum(g => Math.Max(g.RejectedQty, g.ReceivedQty - g.RejectedQty));
            if (totalAvailable < request.Quantity)
                throw new Exception($"Insufficient quantity to move. Available: {totalAvailable}, Requested: {request.Quantity}");

            // 3. Move Logic
            decimal remainingToMove = request.Quantity;

            foreach (var b in matchingBatches)
            {
                if (remainingToMove <= 0) break;

                decimal batchAvailable = Math.Max(b.RejectedQty, b.ReceivedQty - b.RejectedQty);
                if (batchAvailable <= 0) continue;

                decimal toMove = Math.Min(batchAvailable, remainingToMove);

                // Reduce from source proportionally/sequentially
                decimal remainingFromBatch = toMove;
                if (b.RejectedQty > 0)
                {
                    decimal deductRejected = Math.Min(b.RejectedQty, remainingFromBatch);
                    b.RejectedQty -= deductRejected;
                    b.ReceivedQty -= deductRejected;
                    remainingFromBatch -= deductRejected;
                }
                if (remainingFromBatch > 0)
                {
                    decimal deductAccepted = Math.Min(b.ReceivedQty - b.RejectedQty, remainingFromBatch);
                    b.ReceivedQty -= deductAccepted;
                    if (b.AcceptedQty >= deductAccepted) b.AcceptedQty -= deductAccepted;
                }

                remainingToMove -= toMove;

                // 4. Update or Create at the target location
                var existingTargetBatch = await _context.GRNDetails
                    .FirstOrDefaultAsync(g => g.ProductId == request.ProductId &&
                                            g.WarehouseId == request.TargetWarehouseId &&
                                            g.RackId == request.TargetRackId &&
                                            g.ExpDate == b.ExpDate &&
                                            g.UnitRate == b.UnitRate &&
                                            g.CompanyId == companyId, ct);

                if (existingTargetBatch != null)
                {
                    if (isTargetExpired)
                    {
                        existingTargetBatch.ReceivedQty += toMove;
                        existingTargetBatch.RejectedQty += toMove;
                        existingTargetBatch.AcceptedQty = 0;
                    }
                    else
                    {
                        existingTargetBatch.ReceivedQty += toMove;
                        existingTargetBatch.AcceptedQty += toMove;
                    }
                }
                else
                {
                    var newBatch = new GRNDetail
                    {
                        GRNHeaderId = b.GRNHeaderId,
                        ProductId = b.ProductId,
                        CompanyId = companyId,
                        ReceivedQty = toMove,
                        UnitRate = b.UnitRate,
                        GstPercent = b.GstPercent,
                        MfgDate = b.MfgDate,
                        ExpDate = b.ExpDate,
                        RackId = request.TargetRackId,
                        WarehouseId = request.TargetWarehouseId,
                        OrderedQty = 0,
                        AcceptedQty = isTargetExpired ? 0 : toMove,
                        RejectedQty = isTargetExpired ? toMove : 0,
                        PendingQty = 0
                    };
                    _context.GRNDetails.Add(newBatch);
                }
            }

            // 5. Record Inventory Transactions for Audit Trail
            var outTx = new InventoryTransaction(
                request.ProductId,
                request.Quantity,
                "RackMove-OUT",
                "MOVE-" + DateTime.Now.Ticks.ToString().Substring(10),
                request.SourceWarehouseId,
                request.SourceRackId,
                request.ExpiryDate,
                request.ExpiryDate,
                companyId,
                branchId
            );
            await _context.InventoryTransactions.AddAsync(outTx, ct);

            var inTx = new InventoryTransaction(
                request.ProductId,
                request.Quantity,
                "RackMove-IN",
                outTx.ReferenceId,
                request.TargetWarehouseId,
                request.TargetRackId,
                request.ExpiryDate,
                request.ExpiryDate,
                companyId,
                branchId
            );
            await _context.InventoryTransactions.AddAsync(inTx, ct);

            return await _context.SaveChangesAsync(ct) > 0;
        }

        private bool IsExpiredRack(string rackName, string rackDescription)
        {
            var name = (rackName ?? "").ToLower();
            var desc = (rackDescription ?? "").ToLower();
            return name.Contains("e1") || name.Contains("expired") || name.Contains("damaged") || name.Contains("rejected") || name.Contains("purged") ||
                   desc.Contains("expired") || desc.Contains("damaged") || desc.Contains("rejected") || desc.Contains("purged");
        }
    }
}
