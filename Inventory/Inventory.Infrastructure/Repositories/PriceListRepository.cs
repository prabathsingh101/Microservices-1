using Inventory.Application.Common.Interfaces;
using Inventory.Application.PriceLists.DTOs;
using Inventory.Domain.PriceLists;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class PriceListRepository : IPriceListRepository
{
    private readonly InventoryDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public PriceListRepository(InventoryDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
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
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _context.PriceLists.AsNoTracking()
            .Include(x => x.PriceListItems)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId);
    }

    public async Task<List<PriceList>> GetAllAsync()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _context.PriceLists
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .Include(x => x.PriceListItems)
            .ToListAsync();
    }
    public IQueryable<PriceList> Query()
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return _context.PriceLists.Where(x => x.CompanyId == companyId).AsQueryable();
    }
    public void DeleteRange(List<PriceList> PriceLists)
    {
        _context.PriceLists.RemoveRange(PriceLists);
    }

    public async Task<List<PriceList>> GetByIdsAsync(List<Guid> ids)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _context.PriceLists
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id) && x.CompanyId == companyId)
            .ToListAsync();
    }

    public async Task<bool> HasPriceListAsync(List<Guid> pricelistIds)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _context.PriceLists.AsNoTracking()
           .AnyAsync(x => pricelistIds.Contains(x.Id) && x.CompanyId == companyId);
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
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _context.PriceLists.AsNoTracking()
            .Include(p => p.PriceListItems)
                .ThenInclude(i => i.Product) // Ye line zaroori hai
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }
    public async Task UpdatePriceListAsync(PriceList entity, CancellationToken cancellationToken)
    {
        // Existing items ko handle karne ke liye context ka use karein
        _context.PriceLists.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<PriceListItemDto>> GetPriceListItemsAsync(Guid priceListId)
    {
        var companyId = _currentUserService.CompanyId ?? Guid.Empty;
        return await _context.PriceListItems
            .AsNoTracking()
            .Where(x => x.PriceListId == priceListId && x.CompanyId == companyId)
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
