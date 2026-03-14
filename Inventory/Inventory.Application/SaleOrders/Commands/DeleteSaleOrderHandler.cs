using Inventory.Application.Clients;
using Inventory.Application.Common.Interfaces;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.SaleOrders.Commands
{
    public class DeleteSaleOrderHandler : IRequestHandler<DeleteSaleOrderCommand, bool>
    {
        private readonly ISaleOrderRepository _repo;
        private readonly ICustomerClient _customerClient;

        public DeleteSaleOrderHandler(ISaleOrderRepository repo, ICustomerClient customerClient)
        {
            _repo = repo;
            _customerClient = customerClient;
        }

        public async Task<bool> Handle(DeleteSaleOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _repo.GetSaleOrderByIdAsync(request.Id);
            if (order == null) return false;

            bool deleted = false;

            await _repo.ExecuteInTransactionAsync(async () =>
            {
                // 1. If Order was Confirmed, Revert Stock
                if (order.Status == "Confirmed")
                {
                    foreach (var item in order.Items)
                    {
                        // Adding back stock (Positive adjustment)
                        await _repo.UpdateProductStockAsync(item.ProductId, item.Qty);
                    }

                    // 2. Ledger Sync (Reverse Sale)
                    try
                    {
                        // Recording a negative sale to offset the original entry
                        await _customerClient.RecordSaleAsync(
                            order.CustomerId,
                            -order.GrandTotal, // Negative amount
                            order.SoNumber,
                            $"Sale Order Deleted/Cancelled: {order.SoNumber}",
                            "System"
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ledger reversion failed: {ex.Message}");
                        // Note: Microservice failures are often logged but might not rollback the whole DB transaction 
                        // depending on business requirements. Here we continue to delete.
                    }
                }

                // 3. Delete Order and Items
                deleted = await _repo.DeleteAsync(request.Id);
            });

            return deleted;
        }
    }
}
