using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class CreateRfqResponse
{
    public bool Success { get; set; }
    public Guid Id { get; set; }
    public string RfqNo { get; set; } = string.Empty;
}

public record CreateRfqCommand(CreateRfqDto Dto) : IRequest<CreateRfqResponse>;

public class CreateRfqCommandHandler : IRequestHandler<CreateRfqCommand, CreateRfqResponse>
{
    private readonly IInventoryDbContext _context;

    public CreateRfqCommandHandler(IInventoryDbContext context)
    {
        _context = context;
    }

    public async Task<CreateRfqResponse> Handle(CreateRfqCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        // Generate RFQ Number (e.g. RFQ/26-27/0001)
        var count = await _context.RequestForQuotations
            .IgnoreQueryFilters()
            .Where(x => x.CompanyId == dto.CompanyId)
            .CountAsync(ct);

        int currentYear = DateTime.UtcNow.Year;
        int nextYear = currentYear + 1;
        string rfqNo = $"RFQ/{currentYear % 100:D2}-{nextYear % 100:D2}/{(count + 1):D4}";

        var rfq = new RequestForQuotation
        {
            Id = Guid.NewGuid(),
            RfqNo = rfqNo,
            SupplierId = dto.SupplierId,
            SupplierName = dto.SupplierName,
            CreatedDate = DateTime.UtcNow,
            ExpiryDate = dto.ExpiryDate,
            Status = RfqStatus.Draft,
            Remarks = dto.Remarks,
            IsQuick = dto.IsQuick,
            CompanyId = dto.CompanyId,
            BranchId = dto.BranchId,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = dto.CreatedBy
        };

        foreach (var item in dto.Items)
        {
            rfq.Items.Add(new RequestForQuotationItem
            {
                Id = Guid.NewGuid(),
                RfqId = rfq.Id,
                ProductId = item.ProductId,
                Qty = item.Qty,
                UnitPrice = item.UnitPrice,
                TaxRate = item.TaxRate,
                Discount = item.Discount,
                TotalCost = item.TotalCost,
                CompanyId = dto.CompanyId,
                BranchId = dto.BranchId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = dto.CreatedBy
            });
        }

        await _context.RequestForQuotations.AddAsync(rfq, ct);
        var result = await _context.SaveChangesAsync(ct) > 0;

        return new CreateRfqResponse
        {
            Success = result,
            Id = rfq.Id,
            RfqNo = rfqNo
        };
    }
}
