using Inventory.Application.Common.Interfaces;
using Inventory.Application.PriceLists.DTOs;
using Inventory.Application.PriceLists.Queries.GetPriceListById;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetPriceListByIdQueryHandler : IRequestHandler<GetPriceListByIdQuery, PriceListDto>
{
    private readonly IInventoryDbContext _context;

    public GetPriceListByIdQueryHandler(IInventoryDbContext context)
    {
        _context = context;
    }

    public async Task<PriceListDto> Handle(GetPriceListByIdQuery request, CancellationToken cancellationToken)
    {
        // 1. Database se pehle record fetch karein (Bina GroupBy ke taaki SQL error na aaye)
        var entity = await _context.PriceLists
            .AsNoTracking()
            .Include(x => x.PriceListItems)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"PriceList with ID {request.Id} not found.");
        }

        // 2. Memory mein mapping aur Duplicate Cleanup karein (In-Memory GroupBy)
        return new PriceListDto
        {
            id = entity.Id,
            name = entity.Name,
            code = entity.Code,
            priceType = entity.PriceType,
            validFrom = entity.ValidFrom,
            validTo = entity.ValidTo,
            isActive = entity.IsActive,
            remarks = entity.Remarks,
            currency = entity.Currency,
            applicableGroup = entity.ApplicableGroup,

            // Business Fix: Yahan Memory mein duplicates ko filter kar rahe hain
            items = entity.PriceListItems
                .GroupBy(item => item.ProductId) //
                .Select(group => group.First()) // Har Product ka sirf ek hi record dikhayega
                .Select(item => new PriceListItemDetailDto
                {
                    productId = item.ProductId,
                    productName = item.Product?.Name ?? "Unknown Product",
                    unit = item.Unit,
                    rate = item.Rate,
                    discountPercent = item.DiscountPercent,
                    minQty = item.MinQty,
                    maxQty = item.MaxQty
                }).ToList()
        };
    }
}
