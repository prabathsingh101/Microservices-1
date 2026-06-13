using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record GetRfqsPagedQuery(
    int PageIndex,
    int PageSize,
    string? SortField,
    string? SortOrder,
    string? Filter,
    bool IsQuick = false
) : IRequest<PagedResponse<RfqDto>>;

public class GetRfqsPagedQueryHandler : IRequestHandler<GetRfqsPagedQuery, PagedResponse<RfqDto>>
{
    private readonly IInventoryDbContext _context;

    public GetRfqsPagedQueryHandler(IInventoryDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<RfqDto>> Handle(GetRfqsPagedQuery request, CancellationToken ct)
    {
        var query = _context.RequestForQuotations
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .Where(x => x.IsQuick == request.IsQuick)
            .AsNoTracking()
            .AsQueryable();

        // Search Filter
        if (!string.IsNullOrEmpty(request.Filter))
        {
            query = query.Where(x => x.RfqNo.Contains(request.Filter) || 
                                     (x.SupplierName != null && x.SupplierName.Contains(request.Filter)));
        }

        // Total count
        int total = await query.CountAsync(ct);

        // Sorting
        if (!string.IsNullOrEmpty(request.SortField))
        {
            bool isDesc = request.SortOrder == "desc" || request.SortOrder == "-1";
            switch (request.SortField.ToLower())
            {
                case "rfqno":
                    query = isDesc ? query.OrderByDescending(x => x.RfqNo) : query.OrderBy(x => x.RfqNo);
                    break;
                case "suppliername":
                    query = isDesc ? query.OrderByDescending(x => x.SupplierName) : query.OrderBy(x => x.SupplierName);
                    break;
                case "createddate":
                    query = isDesc ? query.OrderByDescending(x => x.CreatedDate) : query.OrderBy(x => x.CreatedDate);
                    break;
                case "status":
                    query = isDesc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status);
                    break;
                default:
                    query = query.OrderByDescending(x => x.CreatedDate);
                    break;
            }
        }
        else
        {
            query = query.OrderByDescending(x => x.CreatedDate);
        }

        var list = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = list.Select(RfqDto.FromEntity).ToList();

        // Fetch corresponding PO details for Converted RFQs
        var convertedRfqNos = dtos
            .Where(x => x.Status == (int)Inventory.Domain.Enums.RfqStatus.Converted)
            .Select(x => x.RfqNo)
            .ToList();

        if (convertedRfqNos.Any())
        {
            var pos = await _context.PurchaseOrders
                .AsNoTracking()
                .Where(po => po.Remarks != null)
                .Select(po => new { po.Id, po.PoNumber, po.Remarks })
                .ToListAsync(ct);

            foreach (var dto in dtos.Where(x => x.Status == (int)Inventory.Domain.Enums.RfqStatus.Converted))
            {
                var matchingPo = pos.FirstOrDefault(po => po.Remarks!.Contains($"Converted from RFQ No: {dto.RfqNo}"));
                if (matchingPo != null)
                {
                    dto.ConvertedPoNumber = matchingPo.PoNumber;
                    dto.ConvertedPoId = matchingPo.Id;
                }
            }
        }

        return new PagedResponse<RfqDto>(dtos, total, request.PageIndex, request.PageSize);
    }
}
