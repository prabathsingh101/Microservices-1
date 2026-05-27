using Inventory.Application.Clients;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.SalesInvoices.Commands.DeleteSalesInvoice
{
    public class DeleteSalesInvoiceHandler : IRequestHandler<DeleteSalesInvoiceCommand, bool>
    {
        private readonly IInventoryDbContext _context;
        private readonly ICustomerClient _customerClient;

        public DeleteSalesInvoiceHandler(IInventoryDbContext context, ICustomerClient customerClient)
        {
            _context = context;
            _customerClient = customerClient;
        }

        public async Task<bool> Handle(DeleteSalesInvoiceCommand request, CancellationToken cancellationToken)
        {
            var invoice = await _context.SalesInvoices
                .Include(si => si.Items)
                .FirstOrDefaultAsync(si => si.Id == request.Id, cancellationToken);

            if (invoice == null) return false;

            bool deleted = false;

            // Strategy: Use a transaction to ensure stock, audit, ledger, and status all update together
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // 1. If Invoice was Confirmed/Delivered/Completed, Revert Stock
                    if (invoice.Status == "Confirmed" || invoice.Status == "Delivered" || invoice.Status == "Completed")
                    {
                        foreach (var item in invoice.Items)
                        {
                            // 🆕 Record Reversal in Audit Trail
                            var reversalTx = new InventoryTransaction(
                                item.ProductId,
                                item.Qty, // Positive because it is READDING stock
                                "TaxInvoice-DELETED",
                                invoice.InvoiceNo,
                                item.WarehouseId,
                                item.RackId,
                                item.MfgDate,
                                item.ExpDate,
                                invoice.CompanyId,
                                invoice.BranchId
                            );
                            await _context.InventoryTransactions.AddAsync(reversalTx, cancellationToken);

                            // 🚀 RESTORE PHYSICAL WAREHOUSE STOCK
                            if (item.WarehouseId.HasValue && item.WarehouseId != Guid.Empty)
                            {
                                var whStock = await _context.WarehouseStocks
                                    .FirstOrDefaultAsync(ws => ws.ProductId == item.ProductId && ws.WarehouseId == item.WarehouseId, cancellationToken);

                                if (whStock != null)
                                {
                                    whStock.Quantity += item.Qty;
                                }
                            }
                        }

                        // 2. Ledger Sync (Reverse Sale)
                        if (invoice.CustomerId.HasValue && invoice.CustomerId.Value != Guid.Empty)
                        {
                            try
                            {
                                string ledgerNote = $"Sales Invoice Deleted/Cancelled: {invoice.InvoiceNo}";
                                if (!string.IsNullOrWhiteSpace(request.Reason))
                                {
                                    ledgerNote += $" | Reason: {request.Reason}";
                                }

                                // Recording a negative sale to offset the original entry
                                await _customerClient.RecordSaleAsync(
                                    invoice.CustomerId.Value,
                                    -invoice.GrandTotal, // Negative amount
                                    invoice.InvoiceNo,
                                    ledgerNote,
                                    "System",
                                    Guid.TryParse(invoice.BranchId, out var branchId) ? branchId : (Guid?)null,
                                    invoice.CompanyId
                                );
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ledger reversion failed for Sales Invoice: {ex.Message}");
                            }
                        }
                    }

                    // 3. Soft Delete: Update Invoice Entity to Cancelled
                    invoice.Status = "Cancelled";
                    if (!string.IsNullOrWhiteSpace(request.Reason))
                    {
                        invoice.Remarks += $" [Cancelled Reason: {request.Reason}]";
                    }
                    
                    _context.SalesInvoices.Update(invoice);
                    await _context.SaveChangesAsync(cancellationToken);
                    
                    await transaction.CommitAsync(cancellationToken);
                    deleted = true;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return deleted;
        }
    }
}
