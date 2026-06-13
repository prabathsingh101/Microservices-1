using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record ConfirmRfqItemRateDto(
    Guid ItemId,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Discount,
    decimal TotalCost
);

public record ConfirmRfqRatesCommand(
    Guid RfqId,
    string UserEmail,
    List<ConfirmRfqItemRateDto> ItemRates
) : IRequest<bool>;

public class ConfirmRfqRatesCommandHandler : IRequestHandler<ConfirmRfqRatesCommand, bool>
{
    private readonly IInventoryDbContext _context;

    public ConfirmRfqRatesCommandHandler(IInventoryDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ConfirmRfqRatesCommand request, CancellationToken ct)
    {
        var rfq = await _context.RequestForQuotations
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.RfqId, ct);

        if (rfq == null) return false;

        // Verify status is Sent or Confirmed before confirming rates
        if (rfq.Status != RfqStatus.Sent && rfq.Status != RfqStatus.Confirmed)
        {
            throw new Exception("Only Sent or Confirmed RFQs can have their rates confirmed.");
        }

        // Update item rates
        foreach (var rateDto in request.ItemRates)
        {
            var item = rfq.Items.FirstOrDefault(i => i.Id == rateDto.ItemId);
            if (item != null)
            {
                item.UnitPrice = rateDto.UnitPrice;
                item.TaxRate = rateDto.TaxRate;
                item.Discount = rateDto.Discount;
                item.TotalCost = rateDto.TotalCost;
                item.ModifiedOn = DateTime.UtcNow;
                item.ModifiedBy = request.UserEmail;
            }
        }

        // Update status to Confirmed
        rfq.Status = RfqStatus.Confirmed;
        rfq.ModifiedOn = DateTime.UtcNow;
        rfq.ModifiedBy = request.UserEmail;

        return await _context.SaveChangesAsync(ct) > 0;
    }
}
