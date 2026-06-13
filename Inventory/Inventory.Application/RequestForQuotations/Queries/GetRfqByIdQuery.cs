using System;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record GetRfqByIdQuery(Guid Id) : IRequest<RfqDto?>;

public class GetRfqByIdQueryHandler : IRequestHandler<GetRfqByIdQuery, RfqDto?>
{
    private readonly IInventoryDbContext _context;

    public GetRfqByIdQueryHandler(IInventoryDbContext context)
    {
        _context = context;
    }

    public async Task<RfqDto?> Handle(GetRfqByIdQuery request, CancellationToken ct)
    {
        var rfq = await _context.RequestForQuotations
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (rfq == null) return null;

        var dto = RfqDto.FromEntity(rfq);

        if (dto.Status == (int)Inventory.Domain.Enums.RfqStatus.Converted)
        {
            var matchingPo = await _context.PurchaseOrders
                .AsNoTracking()
                .Where(po => po.Remarks != null && po.Remarks.Contains($"Converted from RFQ No: {dto.RfqNo}"))
                .Select(po => new { po.Id, po.PoNumber })
                .FirstOrDefaultAsync(ct);

            if (matchingPo != null)
            {
                dto.ConvertedPoNumber = matchingPo.PoNumber;
                dto.ConvertedPoId = matchingPo.Id;
            }
        }

        return dto;
    }
}
