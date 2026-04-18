using Inventory.Application.Common.Interfaces;
using Inventory.Application.PriceLists.Commands.UpdatePriceList;
using Inventory.Domain.PriceLists;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class UpdatePriceListCommandHandler : IRequestHandler<UpdatePriceListCommand, bool>
{
    private readonly IPriceListRepository _repository;
    private readonly IInventoryDbContext _context;

    public UpdatePriceListCommandHandler(IPriceListRepository repository, IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<bool> Handle(UpdatePriceListCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch Entity with Items (Tracking ON rakhein update ke liye)
        var entity = await _context.PriceLists
            .Include(x => x.PriceListItems)
            .FirstOrDefaultAsync(x => x.Id == request.id, cancellationToken);

        if (entity == null) return false;

        // 1.1 Duplicate Name Check
        var isDuplicateName = await _context.PriceLists
            .AnyAsync(x => x.Name.ToLower() == request.name.ToLower() && x.Id != request.id, cancellationToken);
        
        if (isDuplicateName)
        {
            throw new Exception($"Duplicate Name: Price List '{request.name}' already exists.");
        }

        // 1.2 Duplicate Code Check
        var isDuplicateCode = await _context.PriceLists
            .AnyAsync(x => x.Code.ToLower() == request.code.ToLower() && x.Id != request.id, cancellationToken);
        
        if (isDuplicateCode)
        {
            throw new Exception($"Duplicate Code: Price List with code '{request.code}' already exists.");
        }

        // 2. Global Validation: Kya koi aur list ACTIVE hai?
        if (request.isActive)
        {
            foreach (var item in request.priceListItems)
            {
                var isAlreadyActiveElsewhere = await _context.PriceListItems
                    .AnyAsync(pi => pi.ProductId == item.productId &&
                                    pi.PriceList.PriceType == request.priceType &&
                                    pi.PriceList.IsActive == true &&
                                    pi.PriceListId != request.id, cancellationToken);

                if (isAlreadyActiveElsewhere)
                {
                    throw new Exception($"Product ID {item.productId} is already assigned to another ACTIVE list.");
                }
            }
        }

        // 3. Header Update
        entity.Name = request.name;
        entity.Code = request.code;
        entity.PriceType = request.priceType;
        entity.ValidFrom = request.validFrom;
        entity.ValidTo = request.validTo;
        entity.IsActive = request.isActive;
        entity.Remarks = request.remarks;

        // 4. SYNC LOGIC (Delete nahi, Sync karein)
        // Pehle wo items hatayein jo request mein nahi hain (UI se delete kiye gaye)
        var itemsToRemove = entity.PriceListItems
            .Where(existing => !request.priceListItems.Any(req => req.productId == existing.ProductId))
            .ToList();

        foreach (var item in itemsToRemove)
        {
            _context.PriceListItems.Remove(item);
        }

        // Ab naye items add karein ya purano ko update karein
        foreach (var itemDto in request.priceListItems)
        {
            var existingItem = entity.PriceListItems
                .FirstOrDefault(x => x.ProductId == itemDto.productId);

            if (existingItem != null)
            {
                // Purana item hai? Toh sirf rate/qty update karein
                existingItem.Rate = itemDto.rate;
                existingItem.DiscountPercent = itemDto.discountPercent;
                existingItem.MinQty = itemDto.minQty;
                existingItem.MaxQty = itemDto.maxQty;
                existingItem.Unit = itemDto.unit;
            }
            else
            {
                // Naya item hai? Toh add karein
                entity.PriceListItems.Add(new PriceListItem
                {
                    PriceListId = entity.Id,
                    ProductId = itemDto.productId,
                    Rate = itemDto.rate,
                    DiscountPercent = itemDto.discountPercent,
                    MinQty = itemDto.minQty,
                    MaxQty = itemDto.maxQty,
                    Unit = itemDto.unit,
                    CompanyId = request.companyId
                });
            }
        }

        // 5. Final Save
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
