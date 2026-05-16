using Inventory.Application.Clients;
using Inventory.Application.Common.Interfaces;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Entities;

namespace Inventory.Application.SaleOrders.Commands
{
    public class DeleteSaleOrderHandler : IRequestHandler<DeleteSaleOrderCommand, bool>
    {
        private readonly ISaleOrderRepository _repo;
        private readonly ICustomerClient _customerClient;
        private readonly IInventoryDbContext _context;

        public DeleteSaleOrderHandler(ISaleOrderRepository repo, ICustomerClient customerClient, IInventoryDbContext context)
        {
            _repo = repo;
            _customerClient = customerClient;
            _context = context;
        }

        public async Task<bool> Handle(DeleteSaleOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _repo.GetSaleOrderByIdAsync(request.Id);
            if (order == null) return false;

            bool deleted = false;

            await _repo.ExecuteInTransactionAsync(async () =>
            {
                // 1. If Order was Confirmed/Delivered/Completed, Revert Stock
                if (order.Status == "Confirmed" || order.Status == "Delivered" || order.Status == "Completed")
                {
                    foreach (var item in order.Items)
                    {
                        // 🆕 Record Reversal in Audit Trail
                        var reversalTx = new InventoryTransaction(
                            item.ProductId,
                            item.Qty, // Positive because it is READDING stock
                            (order.IsQuick ? "QuickSale" : "Sale") + "-DELETED",
                            order.SoNumber,
                            item.WarehouseId,
                            item.RackId,
                            item.ManufacturingDate,
                            item.ExpiryDate,
                            order.CompanyId,
                            order.BranchId
                        );
                        await _context.InventoryTransactions.AddAsync(reversalTx);

                        // 🚀 RESTORE PHYSICAL WAREHOUSE STOCK
                        if (item.WarehouseId.HasValue && item.WarehouseId != Guid.Empty)
                        {
                            var whStock = await _context.WarehouseStocks
                                .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == item.WarehouseId);

                            if (whStock != null)
                            {
                                whStock.Quantity += item.Qty;
                            }
                        }
                    }

                    // 2. Ledger Sync (Reverse Sale)
                    if (order.CustomerId.HasValue && order.CustomerId.Value != Guid.Empty)
                    {
                        try
                        {
                            // Recording a negative sale to offset the original entry
                            await _customerClient.RecordSaleAsync(
                                order.CustomerId.Value,
                                -order.GrandTotal, // Negative amount
                                order.SoNumber,
                                $"Sale Order Deleted/Cancelled: {order.SoNumber}",
                                "System",
                                Guid.TryParse(order.BranchId, out var branchId) ? branchId : (Guid?)null,
                                order.CompanyId
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ledger reversion failed: {ex.Message}");
                            // Note: Microservice failures are often logged but might not rollback the whole DB transaction 
                            // depending on business requirements. Here we continue to delete.
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[DeleteSaleOrder] Skipping ledger sync for Walking Customer: {order.GuestName}");
                    }
                }

                // 3. Delete Order and Items
                deleted = await _repo.DeleteAsync(request.Id);
            });

            return deleted;
        }
    }
}
