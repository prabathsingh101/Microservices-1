using Inventory.Application.Common.Interfaces;
using Inventory.Application.GatePasses.DTOs;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.GatePasses.Commands.CreateGatePass
{
    public class CreateGatePassCommandHandler : IRequestHandler<CreateGatePassCommand, GatePassDto>
    {
        private readonly IInventoryDbContext _context;

        public CreateGatePassCommandHandler(IInventoryDbContext context)
        {
            _context = context;
        }

        public async Task<GatePassDto> Handle(CreateGatePassCommand request, CancellationToken cancellationToken)
        {
            var year = DateTime.UtcNow.Year;
            var isInward = request.PassType == "Inward";
            var prefix = isInward ? "GP-IN" : "GP";

            // Increment sequence based on the last generated number for this year/type
            var lastPass = await _context.GatePasses
                .Where(x => x.PassNo.StartsWith($"{prefix}-{year}"))
                .OrderByDescending(x => x.PassNo)
                .FirstOrDefaultAsync(cancellationToken);

            int nextSequence = 1;
            if (lastPass != null)
            {
                var parts = lastPass.PassNo.Split('-');
                if (int.TryParse(parts.Last(), out int lastSeq))
                {
                    nextSequence = lastSeq + 1;
                }
            }

            var passNo = $"{prefix}-{year}-{nextSequence.ToString("D4")}";

            var entity = new GatePass
            {
                PassNo = passNo,
                PassType = request.PassType,
                ReferenceType = request.ReferenceType,
                ReferenceId = request.ReferenceId,
                ReferenceNo = request.ReferenceNo,
                InvoiceNo = request.InvoiceNo,
                PartyName = request.PartyName,
                VehicleNo = request.VehicleNo,
                VehicleType = request.VehicleType,
                DriverName = request.DriverName,
                DriverPhone = request.DriverPhone,
                TransporterName = request.TransporterName,
                TotalQty = request.TotalQty,
                TotalWeight = request.TotalWeight,
                GateEntryTime = request.GateEntryTime,
                SecurityGuard = request.SecurityGuard,
                Status = request.Status, // 1 = Entered/Created
                Remarks = request.Remarks
            };

            _context.GatePasses.Add(entity);

            // --- NEW: Update Reference Table with GatePassNo ---
            if (request.ReferenceType == 3) // 3 = SaleOrder
            {
                var ids = request.ReferenceId.Split(',')
                    .Select(id => Guid.TryParse(id, out Guid parsedId) ? parsedId : Guid.Empty)
                    .Where(id => id != Guid.Empty)
                    .ToList();
                var saleOrders = await _context.SaleOrders.Where(s => ids.Contains(s.Id)).ToListAsync(cancellationToken);
                foreach (var so in saleOrders)
                {
                    so.GatePassNo = entity.PassNo;
                }
            }
            else if (request.ReferenceType == 5) // 5 = SaleReturn
            {
                var ids = request.ReferenceId.Split(',')
                    .Select(id => Guid.TryParse(id, out Guid parsedId) ? parsedId : Guid.Empty)
                    .Where(id => id != Guid.Empty)
                    .ToList();
                var saleReturns = await _context.SaleReturnHeaders.Where(s => ids.Contains(s.Id)).ToListAsync(cancellationToken);
                foreach (var sr in saleReturns)
                {
                    sr.GatePassNo = entity.PassNo;
                    sr.Status = "INWARDED"; // Status update for consistency [cite: 2026-02-23]
                }
            }
            else if (request.ReferenceType == 4) // 4 = PurchaseReturn
            {
                var ids = request.ReferenceId.Split(',')
                    .Select(id => Guid.TryParse(id, out Guid parsedId) ? parsedId : Guid.Empty)
                    .Where(id => id != Guid.Empty)
                    .ToList();
                var purchaseReturns = await _context.PurchaseReturns.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
                foreach (var pr in purchaseReturns)
                {
                    pr.GatePassNo = entity.PassNo;
                }
            }
            else if (request.ReferenceType == 1) // 1 = PurchaseOrder
            {
                var ids = request.ReferenceId.Split(',')
                    .Select(id => Guid.TryParse(id, out Guid parsedId) ? parsedId : Guid.Empty)
                    .Where(id => id != Guid.Empty)
                    .ToList();
                var purchaseOrders = await _context.PurchaseOrders.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
                foreach (var po in purchaseOrders)
                {
                    // po.GatePassNo = entity.PassNo; // Update if property exists
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new GatePassDto
            {
                Id = entity.Id,
                PassNo = entity.PassNo,
                PassType = entity.PassType
            };
        }
    }
}
