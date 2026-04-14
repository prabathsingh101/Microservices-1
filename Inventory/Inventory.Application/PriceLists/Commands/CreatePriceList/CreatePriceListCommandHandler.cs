using Inventory.Application.Common.Interfaces;
using Inventory.Domain.PriceLists;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.PriceLists.Commands.CreatePriceList;

public sealed class CreatePriceListCommandHandler
    : IRequestHandler<CreatePriceListCommand, Guid>
{
    private readonly IPriceListRepository _repository;
    private readonly IInventoryDbContext _context; // DB validation ke liye

    public CreatePriceListCommandHandler(IPriceListRepository repository, IInventoryDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<Guid> Handle(CreatePriceListCommand request, CancellationToken ct)
    {
        // 0. Duplicate Name Check
        var isDuplicateName = await _context.PriceLists
            .AnyAsync(x => x.Name.ToLower() == request.name.ToLower(), ct);

        if (isDuplicateName)
        {
            throw new Exception($"Price List with name '{request.name}' already exists.");
        }

        // 0.1 Duplicate Code Check
        var isDuplicateCode = await _context.PriceLists
            .AnyAsync(x => x.Code.ToLower() == request.code.ToLower(), ct);

        if (isDuplicateCode)
        {
            throw new Exception($"Price List with code '{request.code}' already exists.");
        }

        // 1. Request Level Check (Taaki ek hi form mein duplicate items na hon)
        var internalDuplicates = request.priceListItems
            .GroupBy(x => x.productId)
            .Where(g => g.Count() > 1)
            .Select(y => y.Key)
            .ToList();

        if (internalDuplicates.Any())
        {
            throw new Exception($"Duplicate product detected in the current request list.");
        }

        // 2. Database Validation Rule (Inventory Standard)
        // Hum check kar rahe hain ki kya koi aisi list pehle se DB mein hai jo Active hai
        if (request.isActive)
        {
            foreach (var item in request.priceListItems)
            {
                // Ye query DB ke 'PriceLists' table se 'IsActive' status check karegi
                var alreadyActiveInDb = await _context.PriceListItems
                    .AnyAsync(pi => pi.ProductId == item.productId &&
                                    pi.PriceList.PriceType == request.priceType &&
                                    pi.PriceList.IsActive == true, ct); // DB column check

                if (alreadyActiveInDb)
                {
                    // Agar DB mein pehle se 'Active' record hai, toh naya save nahi hone dega
                    throw new Exception($"Product ID {item.productId} is already marked as ACTIVE in the database for '{request.priceType}' type.");
                }
            }
        }

        // 3. Mapping and Saving logic
        var priceList = new PriceList(
            request.name,
            request.code,
            request.priceType,
            request.applicableGroup,
            request.currency,
            request.validFrom,
            request.validTo,
            request.remarks,
            request.isActive,
            request.createdBy
        );

        foreach (var itemDto in request.priceListItems)
        {
            var item = new PriceListItem(
                priceList.Id,
                itemDto.productId,
                itemDto.rate,
                itemDto.unit,
                itemDto.discountPercent,
                itemDto.minQty,
                itemDto.maxQty
            );
            priceList.PriceListItems.Add(item);
        }

        await _repository.AddAsync(priceList);
        await _context.SaveChangesAsync(ct);

        return priceList.Id;
    }
}
