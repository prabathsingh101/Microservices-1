using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.DeliveryChallans.Commands
{
    public class CancelDeliveryChallanHandler : IRequestHandler<CancelDeliveryChallanCommand, object>
    {
        private readonly IInventoryDbContext _context;

        public CancelDeliveryChallanHandler(IInventoryDbContext context)
        {
            _context = context;
        }

        public async Task<object> Handle(CancelDeliveryChallanCommand request, CancellationToken cancellationToken)
        {
            var challan = await _context.DeliveryChallans
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (challan == null)
                throw new Exception($"Delivery Challan with id '{request.Id}' not found.");

            var status = (challan.Status ?? "").ToLower();

            if (status == "invoiced")
                throw new Exception("This challan has already been invoiced and cannot be cancelled.");

            if (status == "cancelled" || status == "canceled")
                throw new Exception("This challan is already cancelled.");

            // Mark as Cancelled
            challan.Status = "Cancelled";
            challan.CancelReason = request.Reason;

            // Reverse the stock deductions made at challan creation
            foreach (var item in challan.Items)
            {
                if (item.ProductId.HasValue && item.ProductId != Guid.Empty && item.Qty.HasValue)
                {
                    // Add a reversal inventory transaction
                    var reversal = new InventoryTransaction(
                        item.ProductId.Value,
                        item.Qty.Value,          // positive = stock back in
                        "DeliveryChallanCancel",
                        challan.ChallanNo!,
                        item.WarehouseId,
                        item.RackId,
                        item.MfgDate,
                        item.ExpDate,
                        challan.CompanyId,
                        challan.BranchId,
                        null,
                        item.BatchNumber
                    );
                    await _context.InventoryTransactions.AddAsync(reversal, cancellationToken);

                    // Update WarehouseStocks
                    if (item.WarehouseId.HasValue && item.WarehouseId != Guid.Empty)
                    {
                        var whStock = await _context.WarehouseStocks
                            .FirstOrDefaultAsync(ws =>
                                ws.ProductId == item.ProductId &&
                                ws.WarehouseId == item.WarehouseId,
                                cancellationToken);

                        if (whStock != null)
                        {
                            whStock.Quantity += item.Qty.Value;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new { message = $"Delivery Challan '{challan.ChallanNo}' has been cancelled successfully." };
        }
    }
}
