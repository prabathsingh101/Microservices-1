using Inventory.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Stock.Commands
{
    public class SyncStockCommandHandler : IRequestHandler<SyncStockCommand, bool>
    {
        private readonly IInventoryDbContext _context;

        public SyncStockCommandHandler(IInventoryDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(SyncStockCommand request, CancellationToken ct)
        {
            // 1. Fetch all product IDs
            var products = await _context.Products.ToListAsync(ct);
            
            foreach (var product in products)
            {
                // Inward from GRN (Accepted Qty = Received - Rejected)
                decimal totalInward = await _context.GRNDetails
                    .Where(g => g.ProductId == product.Id)
                    .SumAsync(g => g.AcceptedQty, ct);

                // Outward from Sale Orders
                decimal totalOutward = await _context.SaleOrderItems
                    .Where(s => s.ProductId == product.Id && 
                                s.SaleOrder.Status != "Draft" && 
                                s.SaleOrder.Status != "Cancelled")
                    .SumAsync(s => s.Qty, ct);

                // Sale Returns (Coming back into stock)
                decimal totalSaleReturns = await _context.SaleReturnItems
                    .Where(sr => sr.ProductId == product.Id)
                    .SumAsync(sr => sr.ReturnQty, ct);

                // Purchase Returns (Going out of stock)
                decimal totalPurchaseReturns = await _context.PurchaseReturnItems
                    .Where(pr => pr.ProductId == product.Id)
                    .SumAsync(pr => pr.ReturnQty, ct);

                // Global Recalculation
                product.CurrentStock = totalInward - totalOutward + totalSaleReturns - totalPurchaseReturns;
                
                if (product.CurrentStock < 0) product.CurrentStock = 0;
            }

            return await _context.SaveChangesAsync(ct) > 0;
        }
    }
}
