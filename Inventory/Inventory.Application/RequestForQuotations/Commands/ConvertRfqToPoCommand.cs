using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.PurchaseOrders.Queries.GetNextPoNumber;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record ConvertRfqToPoCommand(Guid RfqId, string UserEmail) : IRequest<Guid>;

public class ConvertRfqToPoCommandHandler : IRequestHandler<ConvertRfqToPoCommand, Guid>
{
    private readonly IInventoryDbContext _context;
    private readonly IMediator _mediator;

    public ConvertRfqToPoCommandHandler(IInventoryDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(ConvertRfqToPoCommand request, CancellationToken ct)
    {
        var rfq = await _context.RequestForQuotations
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == request.RfqId, ct);

        if (rfq == null)
        {
            throw new Exception("RFQ not found.");
        }

        if (rfq.Status != RfqStatus.Confirmed)
        {
            throw new Exception("Only Confirmed RFQs can be converted to Purchase Orders.");
        }

        // Get default PriceList for the company to satisfy DB configuration
        var priceList = await _context.PriceLists
            .FirstOrDefaultAsync(x => x.CompanyId == rfq.CompanyId, ct);
        Guid priceListId = priceList?.Id ?? Guid.Empty;

        // Generate next PO Number
        string poNumber = await _mediator.Send(new GetNextPoNumberQuery(rfq.IsQuick), ct);

        // Calculate Totals
        decimal totalQty = rfq.Items.Sum(x => x.Qty);
        decimal grandTotal = rfq.Items.Sum(x => x.TotalCost ?? 0);
        decimal subTotal = rfq.Items.Sum(x => {
            decimal baseAmt = x.Qty * (x.UnitPrice ?? 0);
            decimal discAmt = baseAmt * ((x.Discount ?? 0) / 100m);
            return baseAmt - discAmt;
        });
        decimal totalTax = grandTotal - subTotal;

        // Create Purchase Order in Draft status
        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = poNumber,
            SupplierId = rfq.SupplierId,
            SupplierName = rfq.SupplierName,
            PriceListId = priceListId,
            PoDate = DateTime.UtcNow,
            Status = "Draft",
            Remarks = $"Converted from RFQ No: {rfq.RfqNo}. " + rfq.Remarks,
            IsQuick = rfq.IsQuick,
            TotalQuantity = totalQty,
            SubTotal = subTotal,
            TotalTax = totalTax,
            GrandTotal = grandTotal,
            CompanyId = rfq.CompanyId,
            BranchId = rfq.BranchId,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = request.UserEmail,
            Items = rfq.Items.Select(item => new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                Qty = item.Qty,
                Unit = item.Product?.Unit ?? "PCS",
                Rate = item.UnitPrice ?? 0,
                DiscountPercent = item.Discount ?? 0,
                GstPercent = item.TaxRate ?? 0,
                TaxAmount = item.TotalCost.HasValue && item.UnitPrice.HasValue 
                    ? item.TotalCost.Value - ((item.UnitPrice.Value * item.Qty) - ((item.UnitPrice.Value * item.Qty) * ((item.Discount ?? 0) / 100m))) 
                    : 0,
                Total = item.TotalCost ?? 0,
                CompanyId = rfq.CompanyId,
                BranchId = rfq.BranchId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = request.UserEmail
            }).ToList()
        };

        // Recalculate tax breakdowns (CGST/SGST/IGST) using PO entity internal helper
        po.RecalculateTotals();

        // Update RFQ status to Converted
        rfq.Status = RfqStatus.Converted;
        rfq.ModifiedOn = DateTime.UtcNow;
        rfq.ModifiedBy = request.UserEmail;

        await _context.PurchaseOrders.AddAsync(po, ct);
        await _context.SaveChangesAsync(ct);

        return po.Id;
    }
}
