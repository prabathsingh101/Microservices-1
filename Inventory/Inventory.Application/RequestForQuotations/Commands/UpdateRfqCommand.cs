using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record UpdateRfqCommand(UpdateRfqDto Dto) : IRequest<bool>;

public class UpdateRfqCommandHandler : IRequestHandler<UpdateRfqCommand, bool>
{
    private readonly IInventoryDbContext _context;

    public UpdateRfqCommandHandler(IInventoryDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateRfqCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        var rfq = await _context.RequestForQuotations
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

        if (rfq == null) return false;

        // Verify status is Draft before making updates
        if (rfq.Status != RfqStatus.Draft)
        {
            throw new Exception("Only RFQs in Draft status can be modified.");
        }

        // Update Header
        rfq.SupplierId = dto.SupplierId;
        rfq.SupplierName = dto.SupplierName;
        rfq.ExpiryDate = dto.ExpiryDate;
        rfq.Remarks = dto.Remarks;
        rfq.ModifiedOn = DateTime.UtcNow;
        rfq.ModifiedBy = dto.ModifiedBy;

        // Sync Items: Remove deleted items
        var itemIdsToKeep = dto.Items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToList();
        var itemsToRemove = rfq.Items.Where(i => !itemIdsToKeep.Contains(i.Id)).ToList();
        foreach (var item in itemsToRemove)
        {
            _context.RequestForQuotationItems.Remove(item);
        }

        // Add or Update Items
        foreach (var itemDto in dto.Items)
        {
            if (itemDto.Id.HasValue)
            {
                // Update existing item
                var existingItem = rfq.Items.FirstOrDefault(i => i.Id == itemDto.Id.Value);
                if (existingItem != null)
                {
                    existingItem.ProductId = itemDto.ProductId;
                    existingItem.Qty = itemDto.Qty;
                    existingItem.UnitPrice = itemDto.UnitPrice;
                    existingItem.TaxRate = itemDto.TaxRate;
                    existingItem.Discount = itemDto.Discount;
                    existingItem.TotalCost = itemDto.TotalCost;
                    existingItem.ModifiedOn = DateTime.UtcNow;
                    existingItem.ModifiedBy = dto.ModifiedBy;
                }
            }
            else
            {
                // Add new item
                rfq.Items.Add(new RequestForQuotationItem
                {
                    Id = Guid.NewGuid(),
                    RfqId = rfq.Id,
                    ProductId = itemDto.ProductId,
                    Qty = itemDto.Qty,
                    UnitPrice = itemDto.UnitPrice,
                    TaxRate = itemDto.TaxRate,
                    Discount = itemDto.Discount,
                    TotalCost = itemDto.TotalCost,
                    CompanyId = rfq.CompanyId,
                    BranchId = rfq.BranchId,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = dto.ModifiedBy
                });
            }
        }

        return await _context.SaveChangesAsync(ct) > 0;
    }
}
