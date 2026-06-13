using System;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record DeleteRfqCommand(Guid Id) : IRequest<bool>;

public class DeleteRfqCommandHandler : IRequestHandler<DeleteRfqCommand, bool>
{
    private readonly IInventoryDbContext _context;

    public DeleteRfqCommandHandler(IInventoryDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteRfqCommand request, CancellationToken ct)
    {
        var rfq = await _context.RequestForQuotations
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (rfq == null) return false;

        // Verify status is Draft before deleting
        if (rfq.Status != RfqStatus.Draft)
        {
            throw new Exception("Only RFQs in Draft status can be deleted.");
        }

        _context.RequestForQuotations.Remove(rfq);
        return await _context.SaveChangesAsync(ct) > 0;
    }
}
