using Inventory.Application.Common.Interfaces;
using Inventory.Application.PriceLists.DTOs;
using Inventory.Application.PriceLists.Queries.GetPriceListById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.PriceLists.Queries.GetPriceListsLookUp;

public sealed class GetPriceListsLookUpQueryHandler
    : IRequestHandler<GetPriceListsLookUpQuery, List<PriceListDto>>
{
    private readonly IInventoryDbContext _context;

    public GetPriceListsLookUpQueryHandler(IInventoryDbContext context)
    {
        _context = context;
    }

    public async Task<List<PriceListDto>> Handle(
        GetPriceListsLookUpQuery request,
        CancellationToken cancellationToken)
    {
        // 1. FAST EXECUTION: .AsNoTracking() use kiya hai
        // 2. STRICT DROPDOWN FILTER: Sirf Active aur Purchase type hi dropdown mein aayega
        return await _context.PriceLists
            .AsNoTracking()
            .Where(pl => pl.IsActive == true && pl.PriceType == "PURCHASE") // Grid par iska koi asar nahi hoga
            .OrderByDescending(pl => pl.CreatedOn)
            .Select(pl => new PriceListDto
            {
                id = pl.Id,
                name = pl.Name,
                code = pl.Code,
                isActive = pl.IsActive, //
                priceType = pl.PriceType
                // Dropdown ke liye jo zaroori fields hain wahi rakhein
            })
            .ToListAsync(cancellationToken);
    }
}
