using Inventory.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.PurchaseOrders.Commands.Delete
{
    // Application/Handlers/BulkDeletePurchaseOrderHandler.cs
    public class BulkDeletePurchaseOrderHandler : IRequestHandler<BulkDeletePurchaseOrderCommand, bool>
    {
        private readonly IPurchaseOrderRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly IInventoryDbContext _context;

        public BulkDeletePurchaseOrderHandler(IPurchaseOrderRepository repo, IUnitOfWork uow, IInventoryDbContext context)
        {
            _repo = repo;
            _uow = uow;
            _context = context;
        }

        public async Task<bool> Handle(BulkDeletePurchaseOrderCommand request, CancellationToken ct)
        {
            if (request.Ids == null || !request.Ids.Any()) return false;

            // Saare selected orders ek saath fetch karna
            var orders = await _repo.GetByIdsAsync(request.Ids);

            foreach (var order in orders)
            {
                // Hamara Domain Rule: Draft mode check
                // Agar ek bhi PO 'Received' hua toh ye exception throw kar dega
                order.CanBeDeleted();

                // Revert RFQ Status if converted from RFQ
                if (!string.IsNullOrEmpty(order.Remarks) && order.Remarks.Contains("Converted from RFQ No: "))
                {
                    try
                    {
                        int startIndex = order.Remarks.IndexOf("Converted from RFQ No: ") + "Converted from RFQ No: ".Length;
                        int endIndex = order.Remarks.IndexOf(".", startIndex);
                        string rfqNo = "";
                        if (endIndex > startIndex)
                        {
                            rfqNo = order.Remarks.Substring(startIndex, endIndex - startIndex).Trim();
                        }
                        else if (order.Remarks.Length >= startIndex + 14)
                        {
                            rfqNo = order.Remarks.Substring(startIndex, 14).Trim();
                        }

                        if (rfqNo.StartsWith("RFQ/"))
                        {
                            var rfq = await _context.RequestForQuotations
                                .FirstOrDefaultAsync(x => x.RfqNo == rfqNo, ct);
                            if (rfq != null && rfq.Status == Inventory.Domain.Enums.RfqStatus.Converted)
                            {
                                rfq.Status = Inventory.Domain.Enums.RfqStatus.Confirmed;
                                rfq.ModifiedOn = DateTime.UtcNow;
                                rfq.ModifiedBy = order.ModifiedBy ?? "System";
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Eagerly fail-safe
                    }
                }

                // Repository ko bolna ki isey delete list mein daal de
                _repo.Delete(order);
            }

            // Saare records ek hi transaction mein commit honge
            return await _uow.SaveChangesAsync(ct) > 0;
        }
    }
}
