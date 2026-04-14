using Inventory.Application.PriceLists.DTOs;
using Inventory.Domain.Entities;
using Inventory.Domain.PriceLists;

namespace Inventory.Application.Common.Interfaces;

public interface IPriceListRepository
{
    Task AddAsync(PriceList priceList);
    Task UpdateAsync(PriceList priceList);
    Task DeleteAsync(PriceList priceList);

    Task<PriceList?> GetByIdAsync(Guid id);
    Task<List<PriceList>> GetAllAsync();
    IQueryable<PriceList> Query();
    void DeleteRange(List<PriceList> subcategories);
    Task<List<PriceList>> GetByIdsAsync(List<Guid> ids);
    Task<bool> HasPriceListAsync(List<Guid> pricelistIds);

    Task AddAsync(PriceList priceList, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task<PriceList?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken);

    Task UpdatePriceListAsync(PriceList entity, CancellationToken cancellationToken);

    Task<List<PriceListItemDto>> GetPriceListItemsAsync(Guid priceListId);

}
