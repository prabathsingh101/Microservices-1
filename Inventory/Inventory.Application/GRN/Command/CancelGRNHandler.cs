using Inventory.Application.Clients;
using Inventory.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.GRN.Command
{
    public class CancelGRNHandler : IRequestHandler<CancelGRNCommand, bool>
    {
        private readonly IGRNRepository _repo;
        private readonly IServiceScopeFactory _scopeFactory;

        public CancelGRNHandler(
            IGRNRepository repo,
            IServiceScopeFactory scopeFactory)
        {
            _repo = repo;
            _scopeFactory = scopeFactory;
        }

        public async Task<bool> Handle(CancelGRNCommand request, CancellationToken ct)
        {
            // We will fetch the GRN using the existing print method or a new one
            var grnHeader = await _repo.GetGrnBasicDetailsAsync(request.GrnId);
            if (grnHeader == null)
            {
                throw new Exception("GRN not found");
            }

            // Reverse stock
            bool stockReversed = await _repo.CancelGRNWithStockReversal(request.GrnId);

            if (stockReversed && grnHeader.SupplierId != Guid.Empty && grnHeader.TotalAmount > 0)
            {
                // Reverse Ledger (Supplier Credit Reversal)
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var supplierClient = scope.ServiceProvider.GetRequiredService<ISupplierClient>();
                    
                    string description = $"Cancellation Reversal for GRN: {grnHeader.GRNNumber}";
                    if (!string.IsNullOrWhiteSpace(request.Reason))
                    {
                        description += $" | Reason: {request.Reason}";
                    }
                    // RecordPurchaseReturnAsync can be used to reverse the ledger
                    await supplierClient.RecordPurchaseReturnAsync(grnHeader.SupplierId, grnHeader.TotalAmount, grnHeader.GRNNumber ?? "N/A", description, request.CancelledBy);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CancelGRNHandler] WARNING: Financial reversal failed: {ex.Message}");
                }
            }

            return stockReversed;
        }
    }
}
