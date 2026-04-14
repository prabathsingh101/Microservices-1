using Inventory.Application.Common.Interfaces;
using Inventory.Application.PriceLists.DTOs;
using Inventory.Domain.PriceLists;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class PriceListRepository : IPriceListRepository
{
    private readonly InventoryDbContext _context;

    public PriceListRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PriceList priceList)
    {
        await _context.PriceLists.AddAsync(priceList);
        await _context.SaveChangesAsync();
    }

    public Task UpdateAsync(PriceList priceList)
    {
        _context.PriceLists.Update(priceList);
        return Task.CompletedTask;
    }

    // ? THIS IS THE METHOD YOU ASKED FOR
    public Task DeleteAsync(PriceList priceList)
    {
        _context.PriceLists.Remove(priceList);
        return Task.CompletedTask;
    }

    public async Task<PriceList?> GetByIdAsync(Guid id)
    {
        return await _context.PriceLists.AsNoTracking()
            .Include(x => x.PriceListItems)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<PriceList>> GetAllAsync()
    {
        return await _context.PriceLists
            .AsNoTracking()
            .Include(x => x.PriceListItems)
            .ToListAsync();
    }
    public IQueryable<PriceList> Query()
    {
        return _context.PriceLists.AsQueryable();
    }
    public void DeleteRange(List<PriceList> PriceLists)
    {
        _context.PriceLists.RemoveRange(PriceLists);
    }

    public async Task<List<PriceList>> GetByIdsAsync(List<Guid> ids)
    {
        return await _context.PriceLists
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();
    }

    public async Task<bool> HasPriceListAsync(List<Guid> pricelistIds)
    {
        return await _context.PriceLists.AsNoTracking()
           .AnyAsync(x => pricelistIds.Contains(x.Id));
    }

    public async Task AddAsync(PriceList priceList, CancellationToken ct)
    {
        await _context.PriceLists.AddAsync(priceList, ct);
    }
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        // Actual SQL 'INSERT' command yahan chalegi
        await _context.SaveChangesAsync(ct);
    }

    public async Task<PriceList?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.PriceLists.AsNoTracking()
            .Include(p => p.PriceListItems)
                .ThenInclude(i => i.Product) // Ye line zaroori hai
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
    public async Task UpdatePriceListAsync(PriceList entity, CancellationToken cancellationToken)
    {
        // Existing items ko handle karne ke liye context ka use karein
        _context.PriceLists.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<PriceListItemDto>> GetPriceListItemsAsync(Guid priceListId)
    {
        return await _context.PriceListItems
            .AsNoTracking()
            .Where(x => x.PriceListId == priceListId)
            .Select(x => new PriceListItemDto
            {
                ProductId = x.ProductId,
                ProductName = x.Product.Name,
                Rate = x.Rate, // Price list ka current rate
                Unit = x.Product.Unit
            })
            .ToListAsync();
    }
}
