using Inventory.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Clients;
using Microsoft.AspNetCore.Http;

namespace Inventory.Application.PurchaseOrders.Commands.MakePayment
{
    public class RecordPurchasePaymentCommandHandler : IRequestHandler<RecordPurchasePaymentCommand, bool>
    {
        private readonly IInventoryDbContext _context;
        private readonly ISupplierClient _supplierClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RecordPurchasePaymentCommandHandler(
            IInventoryDbContext context,
            ISupplierClient supplierClient,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _supplierClient = supplierClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> Handle(RecordPurchasePaymentCommand request, CancellationToken cancellationToken)
        {
            var po = await _context.PurchaseOrders
                .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId, cancellationToken);

            if (po == null)
            {
                throw new Exception("Purchase Order not found.");
            }

            po.PaidAmount += request.Amount;
            
            // Post Payment to Finance Ledger
            var createdBy = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Admin";
            var description = $"Payment against PO {po.PoNumber}. " + (request.Remarks ?? string.Empty);
            
            var uniqueReference = $"{po.PoNumber}-{DateTime.UtcNow.Ticks % 10000}";

            // This will throw an exception if the API call fails, preventing the DB save below
            await _supplierClient.RecordPaymentAsync(
                po.SupplierId, 
                request.Amount, 
                uniqueReference, 
                description, 
                request.PaymentMode ?? "Cash", 
                createdBy);
            
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
