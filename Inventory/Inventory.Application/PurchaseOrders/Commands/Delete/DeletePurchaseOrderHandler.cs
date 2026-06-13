using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class DeletePurchaseOrderHandler : IRequestHandler<DeletePurchaseOrderCommand, bool>
{
    private readonly IPurchaseOrderRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IInventoryDbContext _context;

    public DeletePurchaseOrderHandler(IPurchaseOrderRepository repo, IUnitOfWork uow, IInventoryDbContext context)
    {
        _repo = repo;
        _uow = uow;
        _context = context;
    }

    public async Task<bool> Handle(DeletePurchaseOrderCommand request, CancellationToken ct)
    {
        var po = await _repo.GetByIdAsync(request.Id);
        if (po == null) return false;

        // Business Rule Check
        po.CanBeDeleted();

        // Revert RFQ Status if converted from RFQ
        if (!string.IsNullOrEmpty(po.Remarks) && po.Remarks.Contains("Converted from RFQ No: "))
        {
            try
            {
                int startIndex = po.Remarks.IndexOf("Converted from RFQ No: ") + "Converted from RFQ No: ".Length;
                int endIndex = po.Remarks.IndexOf(".", startIndex);
                string rfqNo = "";
                if (endIndex > startIndex)
                {
                    rfqNo = po.Remarks.Substring(startIndex, endIndex - startIndex).Trim();
                }
                else if (po.Remarks.Length >= startIndex + 14)
                {
                    rfqNo = po.Remarks.Substring(startIndex, 14).Trim();
                }

                if (rfqNo.StartsWith("RFQ/"))
                {
                    var rfq = await _context.RequestForQuotations
                        .FirstOrDefaultAsync(x => x.RfqNo == rfqNo, ct);
                    if (rfq != null && rfq.Status == Inventory.Domain.Enums.RfqStatus.Converted)
                    {
                        rfq.Status = Inventory.Domain.Enums.RfqStatus.Confirmed;
                        rfq.ModifiedOn = DateTime.UtcNow;
                        rfq.ModifiedBy = po.ModifiedBy ?? "System";
                    }
                }
            }
            catch (Exception)
            {
                // Eagerly fail-safe to not block PO deletion if RFQ revert fails
            }
        }

        _repo.Delete(po);
        return await _uow.SaveChangesAsync(ct) > 0;
    }
}
